using System.Diagnostics;
using System.Reflection;
using System.Text;
using DevDashboard.Core.Hubs;
using DevDashboard.Core.Storage;
using Microsoft.AspNetCore.SignalR;

namespace DevDashboard.Core.Interceptors;

/// <summary>
/// Listens to the EF Core diagnostic source and turns every executed SQL command into a
/// telemetry event tagged with the current request correlation id.
/// </summary>
public sealed class SqlDiagnosticListener : IObserver<DiagnosticListener>, IDisposable
{
    private const string EfCoreListenerName = "Microsoft.EntityFrameworkCore";
    private const string CommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";

    private readonly ITelemetryStore _store;
    private readonly IHubContext<DashboardHub> _hub;
    private readonly List<IDisposable> _subscriptions = new();

    /// <summary>Creates the listener. It only starts collecting once it is subscribed to <see cref="DiagnosticListener.AllListeners"/>.</summary>
    public SqlDiagnosticListener(ITelemetryStore store, IHubContext<DashboardHub> hub)
    {
        _store = store;
        _hub = hub;
    }

    /// <inheritdoc />
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name != EfCoreListenerName) return;
        lock (_subscriptions)
        {
            _subscriptions.Add(listener.Subscribe(new EfCoreObserver(_store, _hub)));
        }
    }

    /// <inheritdoc />
    public void OnError(Exception error) { }

    /// <inheritdoc />
    public void OnCompleted() { }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_subscriptions)
        {
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions.Clear();
        }
    }

    private sealed class EfCoreObserver : IObserver<KeyValuePair<string, object?>>
    {
        private readonly ITelemetryStore _store;
        private readonly IHubContext<DashboardHub> _hub;

        public EfCoreObserver(ITelemetryStore store, IHubContext<DashboardHub> hub)
        {
            _store = store;
            _hub = hub;
        }

        public void OnNext(KeyValuePair<string, object?> pair)
        {
            if (pair.Key != CommandExecuted || pair.Value is null) return;

            try
            {
                var payload = pair.Value;
                var type = payload.GetType();

                var command = type.GetProperty("Command", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(payload) as System.Data.Common.DbCommand;

                var duration = (TimeSpan?)type.GetProperty("Duration", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(payload) ?? TimeSpan.Zero;

                var evt = new TelemetryEvent
                {
                    RequestId = TelemetryContext.RequestId,
                    Type = "sql",
                    DurationMs = (long)duration.TotalMilliseconds,
                    Sql = command?.CommandText,
                    Parameters = BuildParameters(command),
                };

                _store.Add(evt);
                _ = Task.Run(async () =>
                {
                    try { await _hub.Clients.All.SendAsync("ReceiveEvent", evt); } catch { }
                });
            }
            catch
            {
                // Diagnostics must never take the application down.
            }
        }

        public void OnError(Exception error) { }

        public void OnCompleted() { }

        private static string? BuildParameters(System.Data.Common.DbCommand? command)
        {
            if (command?.Parameters is not { Count: > 0 } parameters) return null;

            var builder = new StringBuilder();
            foreach (System.Data.Common.DbParameter parameter in parameters)
            {
                builder.AppendLine($"{parameter.ParameterName} = {parameter.Value}");
            }

            return builder.ToString().TrimEnd();
        }
    }
}
