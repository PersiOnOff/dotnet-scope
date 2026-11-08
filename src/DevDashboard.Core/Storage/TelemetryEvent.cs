namespace DevDashboard.Core.Storage;

/// <summary>
/// A single piece of telemetry: an HTTP request, a SQL command, a log record or an exception.
/// </summary>
public class TelemetryEvent
{
    /// <summary>Unique id of the event. For HTTP events this is also the request correlation id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Correlation id of the HTTP request this event happened during, when any.</summary>
    public Guid? RequestId { get; set; }

    /// <summary>W3C trace id of the <see cref="System.Diagnostics.Activity"/> that was current, when any.</summary>
    public string? TraceId { get; set; }

    /// <summary>W3C span id of the <see cref="System.Diagnostics.Activity"/> that was current, when any.</summary>
    public string? SpanId { get; set; }

    /// <summary>UTC timestamp of the event.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>One of <c>http</c>, <c>sql</c>, <c>log</c> or <c>exception</c>.</summary>
    public string Type { get; set; } = "";

    /// <summary>HTTP method, for request and exception events.</summary>
    public string? Method { get; set; }

    /// <summary>Request path, for request and exception events.</summary>
    public string? Path { get; set; }

    /// <summary>HTTP status code returned to the client.</summary>
    public int? StatusCode { get; set; }

    /// <summary>How long the request or SQL command took, in milliseconds.</summary>
    public long? DurationMs { get; set; }

    /// <summary>Captured request body, truncated to <see cref="DevDashboardOptions.MaxBodyChars"/>.</summary>
    public string? RequestBody { get; set; }

    /// <summary>Captured response body, truncated to <see cref="DevDashboardOptions.MaxBodyChars"/>.</summary>
    public string? ResponseBody { get; set; }

    /// <summary>SQL text executed by EF Core.</summary>
    public string? Sql { get; set; }

    /// <summary>SQL parameters, one <c>name = value</c> pair per line.</summary>
    public string? Parameters { get; set; }

    /// <summary>Log level, for log events.</summary>
    public string? LogLevel { get; set; }

    /// <summary>Logger category, for log events.</summary>
    public string? Category { get; set; }

    /// <summary>Rendered message, or the exception message for exception events.</summary>
    public string? Message { get; set; }

    /// <summary>Full exception text, including the stack trace.</summary>
    public string? Exception { get; set; }
}
