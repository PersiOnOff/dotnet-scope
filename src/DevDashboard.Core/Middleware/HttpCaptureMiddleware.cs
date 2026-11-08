using System.Diagnostics;
using System.Text;
using DevDashboard.Core.Hubs;
using DevDashboard.Core.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace DevDashboard.Core.Middleware;

/// <summary>
/// Captures every HTTP request handled by the application (status, duration, bodies) and
/// publishes it to the dashboard, including the exceptions that escape the pipeline.
/// </summary>
public sealed class HttpCaptureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryStore _store;
    private readonly IHubContext<DashboardHub> _hub;
    private readonly DevDashboardOptions _options;

    /// <summary>Creates the middleware.</summary>
    public HttpCaptureMiddleware(
        RequestDelegate next,
        ITelemetryStore store,
        IHubContext<DashboardHub> hub,
        IOptions<DevDashboardOptions> options)
    {
        _next = next;
        _store = store;
        _hub = hub;
        _options = options.Value;
    }

    /// <summary>Runs the request, times it and publishes the result to the dashboard.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        var requestId = Guid.NewGuid();
        var previousRequestId = TelemetryContext.RequestId;
        TelemetryContext.RequestId = requestId;

        // Captured before the pipeline runs: the hosting Activity (and therefore the trace the
        // application's own spans belong to) is current for the whole request.
        var (traceId, spanId) = TelemetryContext.CurrentSpan();

        var stopwatch = Stopwatch.StartNew();
        var originalBody = context.Response.Body;
        ResponseCaptureStream? capture = null;
        if (_options.CaptureResponseBodies)
        {
            capture = new ResponseCaptureStream(originalBody, _options.MaxBodyChars);
            context.Response.Body = capture;
        }

        string? requestBody = null;
        var failed = false;
        try
        {
            requestBody = await ReadRequestBodyAsync(context.Request);
        }
        catch
        {
            // A body we cannot read must never break the request it belongs to.
            requestBody = null;
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            failed = true;
            stopwatch.Stop();
            AddAndSend(new TelemetryEvent
            {
                RequestId = requestId,
                TraceId = traceId,
                SpanId = spanId,
                Type = "exception",
                Method = context.Request.Method,
                Path = context.Request.Path,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Message = ex.Message,
                Exception = ex.ToString(),
            });
            throw;
        }
        finally
        {
            stopwatch.Stop();
            context.Response.Body = originalBody;
            TelemetryContext.RequestId = previousRequestId;

            // When the exception escaped the pipeline the response may not have started yet, in
            // which case StatusCode still holds the default 200. Report what the client saw.
            var statusCode = failed && !context.Response.HasStarted
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;

            AddAndSend(new TelemetryEvent
            {
                Id = requestId,
                RequestId = requestId,
                TraceId = traceId,
                SpanId = spanId,
                Type = "http",
                Method = context.Request.Method,
                Path = context.Request.Path,
                StatusCode = statusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                RequestBody = requestBody,
                ResponseBody = capture?.GetCapturedText(),
            });
        }
    }

    private bool ShouldSkip(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            var path = context.Request.Path;
            if (!path.StartsWithSegments(DevDashboardDefaults.PathBase) &&
                !(context.Request.Path.Value?.Contains("favicon", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }
        }
        return true;
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!_options.CaptureRequestBodies) return null;
        if (request.ContentLength is null or 0) return null;

        var contentType = request.ContentType ?? "";
        if (!(contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
              contentType.Contains("text", StringComparison.OrdinalIgnoreCase) ||
              contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
              contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        request.EnableBuffering();
        var buffer = new char[Math.Min(Math.Max(_options.MaxBodyChars, 1), 8192)];
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var read = await reader.ReadBlockAsync(buffer.AsMemory(0, buffer.Length));
        request.Body.Position = 0;

        if (read == 0) return null;
        return read == buffer.Length ? new string(buffer, 0, read) + "…" : new string(buffer, 0, read);
    }

    private void AddAndSend(TelemetryEvent evt)
    {
        try { _store.Add(evt); } catch { }
        _ = Task.Run(async () =>
        {
            try { await _hub.Clients.All.SendAsync("ReceiveEvent", evt); } catch { }
        });
    }
}
