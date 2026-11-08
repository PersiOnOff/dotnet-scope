using DevDashboard.Core.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DevDashboard.Core.Middleware;

/// <summary>
/// Gates every dashboard route: 404 when the dashboard is disabled, 403 when the request does not
/// come from the local machine and remote access was not explicitly enabled.
/// </summary>
public sealed class DashboardGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DevDashboardOptions _options;

    /// <summary>Creates the middleware.</summary>
    public DashboardGuardMiddleware(RequestDelegate next, IOptions<DevDashboardOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    /// <summary>Rejects remote callers unless they were explicitly allowed.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(DevDashboardDefaults.PathBase))
        {
            await _next(context);
            return;
        }

        if (!_options.Enabled)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!_options.AllowRemoteAccess && !IsLocal(context.Connection.RemoteIpAddress))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(
                "DevDashboard is restricted to local requests. Set DevDashboardOptions.AllowRemoteAccess = true to change that.");
            return;
        }

        await _next(context);
    }

    private static bool IsLocal(System.Net.IPAddress? address) =>
        address is null || System.Net.IPAddress.IsLoopback(address);
}
