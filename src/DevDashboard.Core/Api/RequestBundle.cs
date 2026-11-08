using DevDashboard.Core.Storage;

namespace DevDashboard.Core.Api;

/// <summary>
/// One HTTP request together with every event captured while it was running. This is the shape
/// the dashboard exports and the shape a script should consume.
/// </summary>
/// <param name="Request">The HTTP event itself.</param>
/// <param name="Related">SQL commands, log records and exceptions correlated with the request.</param>
public sealed record RequestBundle(TelemetryEvent Request, IReadOnlyList<TelemetryEvent> Related);
