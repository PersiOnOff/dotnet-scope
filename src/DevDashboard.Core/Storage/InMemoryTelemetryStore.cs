using System.Collections.Concurrent;

namespace DevDashboard.Core.Storage;

/// <summary>
/// Bounded, thread-safe, in-memory telemetry store. The dashboard is a development tool:
/// telemetry lives in process memory and is gone when the app stops.
/// </summary>
public class InMemoryTelemetryStore : ITelemetryStore
{
    private readonly ConcurrentQueue<TelemetryEvent> _events = new();
    private readonly int _maxEvents;

    /// <summary>Creates a store that keeps at most <paramref name="maxEvents"/> events.</summary>
    public InMemoryTelemetryStore(int maxEvents = 1000)
    {
        if (maxEvents <= 0) throw new ArgumentOutOfRangeException(nameof(maxEvents));
        _maxEvents = maxEvents;
    }

    /// <summary>Number of events currently retained.</summary>
    public int Count => _events.Count;

    /// <inheritdoc />
    public void Add(TelemetryEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        _events.Enqueue(evt);
        while (_events.Count > _maxEvents && _events.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public IReadOnlyList<TelemetryEvent> GetAll() => _events.ToArray();

    /// <inheritdoc />
    public IReadOnlyList<TelemetryEvent> GetByType(string type) =>
        _events.Where(e => string.Equals(e.Type, type, StringComparison.OrdinalIgnoreCase)).ToArray();

    /// <inheritdoc />
    public void Clear()
    {
        while (_events.TryDequeue(out _)) { }
    }
}
