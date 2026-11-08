using DevDashboard.Core.Hubs;
using DevDashboard.Core.Interceptors;
using DevDashboard.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevDashboard.Core.Extensions;

/// <summary>Registration helpers for the embedded dashboard.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the telemetry store, the HTTP/SQL/log capture hooks and the SignalR hub used by
    /// the dashboard. Call this before building the application.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Optional configuration callback.</param>
    public static IServiceCollection AddDevDashboard(
        this IServiceCollection services,
        Action<DevDashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null) services.Configure(configure);
        else services.AddOptions<DevDashboardOptions>();

        services.TryAddSingleton<ITelemetryStore>(sp =>
            new InMemoryTelemetryStore(sp.GetRequiredService<IOptions<DevDashboardOptions>>().Value.MaxEvents));

        services.AddSignalR();
        services.AddSingleton<SqlDiagnosticListener>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DashboardLoggerProvider>());
        return services;
    }
}
