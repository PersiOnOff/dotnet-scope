using DevDashboard.Core;
using DevDashboard.Core.Extensions;
using DevDashboard.Core.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevDashboard.Tests;

/// <summary>
/// A real Kestrel host with the dashboard installed, so tests exercise the actual middleware
/// pipeline instead of a mock.
/// </summary>
internal sealed class TestApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    public HttpClient Client { get; }
    public ITelemetryStore Store => _app.Services.GetRequiredService<ITelemetryStore>();

    private TestApp(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public static async Task<TestApp> StartAsync(Action<DevDashboardOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddDevDashboard(configure);

        var app = builder.Build();
        app.UseDevDashboard();

        app.MapGet("/api/ping", () => Results.Json(new { ok = true }));
        app.MapGet("/api/boom", () =>
        {
            throw new InvalidOperationException("Boom!");
        });
        app.MapGet("/api/log", (ILoggerFactory loggerFactory) =>
        {
            loggerFactory.CreateLogger("Sample.Orders").LogInformation("hello from test");
            return Results.Ok();
        });
        app.MapPost("/api/echo", async (HttpContext context) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            return Results.Text(await reader.ReadToEndAsync());
        });
        app.UseDevDashboardEndpoints();

        await app.StartAsync();

        var address = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .First();

        return new TestApp(app, new HttpClient { BaseAddress = new Uri(address) });
    }

    public TelemetryEvent Last(string type) => Store.GetByType(type).Last();

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
