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
}
