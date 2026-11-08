namespace DevDashboard.Core.Storage;

/// <summary>Read access to the telemetry collected by the dashboard.</summary>
public interface ITelemetryStore
{
    /// <summary>Appends an event, dropping the oldest one when the store is full.</summary>
    void Add(TelemetryEvent evt);

    /// <summary>Snapshots every retained event, oldest first.</summary>
    IReadOnlyList<TelemetryEvent> GetAll();

    /// <summary>Snapshots the retained events of one type, oldest first.</summary>
    IReadOnlyList<TelemetryEvent> GetByType(string type);

    /// <summary>Removes every retained event.</summary>
    void Clear();
}
