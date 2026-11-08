using System.Diagnostics;
using DevDashboard.Core.Api;
using DevDashboard.Core.Hubs;
using DevDashboard.Core.Interceptors;
using DevDashboard.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace DevDashboard.Core.Extensions;

/// <summary>Pipeline helpers for the embedded dashboard.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Serves the dashboard UI from the embedded resources and installs the capture pipeline.
    /// Call this early enough that it wraps the requests you want to see: right after
    /// <c>UseRouting</c> and before your endpoints.
    /// </summary>
    public static IApplicationBuilder UseDevDashboard(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.ApplicationServices.GetRequiredService<IOptions<DevDashboardOptions>>().Value;
        var provider = new ManifestEmbeddedFileProvider(
            typeof(ApplicationBuilderExtensions).Assembly,
            "wwwroot");

        app.UseMiddleware<DashboardGuardMiddleware>();

        if (options.Enabled)
        {
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = provider,
                RequestPath = DevDashboardDefaults.PathBase,
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = provider,
                RequestPath = DevDashboardDefaults.PathBase,
            });

            app.UseMiddleware<HttpCaptureMiddleware>();
        }

        app.UseRouting();

        var sqlListener = app.ApplicationServices.GetService<SqlDiagnosticListener>();
        if (sqlListener is not null)
        {
            DiagnosticListener.AllListeners.Subscribe(sqlListener);
        }

        return app;
    }

    /// <summary>Maps the dashboard hub and JSON API.</summary>
    public static IApplicationBuilder UseDevDashboardEndpoints(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app is IEndpointRouteBuilder endpoints)
        {
            Map(endpoints);
        }
        else
        {
            app.UseEndpoints(Map);
        }

        return app;
    }

    private static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<DashboardHub>(DevDashboardDefaults.HubPath);
        endpoints.MapDashboardApi();
    }
}
