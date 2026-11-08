using DevDashboard.Core.Hubs;
using DevDashboard.Core.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevDashboard.Core.Interceptors;

/// <summary>
/// Forwards <see cref="ILogger"/> output to the dashboard. Categories that would make the
/// dashboard log about itself (SignalR transport, the dashboard itself) or duplicate the HTTP
/// and SQL hooks are excluded to avoid feedback loops and noise.
/// </summary>
public class DashboardLoggerProvider : ILoggerProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string[] _excluded;
    private ITelemetryStore? _store;
    private IHubContext<DashboardHub>? _hub;

    /// <summary>Creates the provider. Dependencies are resolved lazily to avoid a DI cycle at startup.</summary>
    public DashboardLoggerProvider(IServiceProvider serviceProvider, IOptions<DevDashboardOptions> options)
    {
        _serviceProvider = serviceProvider;
        _excluded = options.Value.ExcludedCategories.ToArray();
    }

    private ITelemetryStore Store => _store ??= _serviceProvider.GetRequiredService<ITelemetryStore>();
    private IHubContext<DashboardHub> Hub => _hub ??= _serviceProvider.GetRequiredService<IHubContext<DashboardHub>>();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        if (IsExcluded(categoryName)) return NullLogger.Instance;
        return new DashboardLogger(categoryName, this);
    }

    private bool IsExcluded(string categoryName) =>
        _excluded.Any(prefix => categoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public void Dispose() { }

    private sealed class DashboardLogger : ILogger
    {
        private readonly string _category;
        private readonly DashboardLoggerProvider _provider;

        public DashboardLogger(string category, DashboardLoggerProvider provider)
        {
            _category = category;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var evt = new TelemetryEvent
            {
                RequestId = TelemetryContext.RequestId,
                Type = "log",
                LogLevel = logLevel.ToString(),
                Category = _category,
                Message = formatter(state, exception),
                Exception = exception?.ToString(),
            };

            try { _provider.Store.Add(evt); } catch { }
            _ = Task.Run(async () =>
            {
                try { await _provider.Hub.Clients.All.SendAsync("ReceiveEvent", evt); } catch { }
            });
        }
    }
}
