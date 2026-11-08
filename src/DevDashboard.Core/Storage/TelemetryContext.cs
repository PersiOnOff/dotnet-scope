using System.Diagnostics;

namespace DevDashboard.Core.Storage;

/// <summary>
/// Flows the current request correlation id down the async call chain so that SQL commands and
/// log records emitted while handling a request can be grouped with it.
/// </summary>
public static class TelemetryContext
{
    private static readonly AsyncLocal<Guid?> Current = new();

    /// <summary>Correlation id of the HTTP request currently being handled, if any.</summary>
    public static Guid? RequestId
    {
        get => Current.Value;
        set => Current.Value = value;
    }

    /// <summary>
    /// W3C identifiers of the ambient <see cref="Activity"/>. OpenTelemetry in .NET is built on
    /// <see cref="Activity"/>, so this needs no OpenTelemetry package: any span processor that
    /// creates an Activity is correlated automatically.
    /// </summary>
    public static (string? TraceId, string? SpanId) CurrentSpan()
    {
        var activity = Activity.Current;
        return activity is null
            ? (null, null)
            : (activity.TraceId.ToHexString(), activity.SpanId.ToHexString());
    }
}
