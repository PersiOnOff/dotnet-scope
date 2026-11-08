using System.Net;
using DevDashboard.Core;
using DevDashboard.Core.Interceptors;
using DevDashboard.Core.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevDashboard.Tests;

public class SecurityTests
{
    [Test]
    public async Task Rejects_remote_callers_by_default()
    {
        var context = RemoteContext();
        var middleware = new DashboardGuardMiddleware(_ => Task.CompletedTask, Options.Create(new DevDashboardOptions()));

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Allows_remote_callers_when_explicitly_enabled()
    {
        var context = RemoteContext();
        var middleware = new DashboardGuardMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new DevDashboardOptions { AllowRemoteAccess = true }));

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
    }

    [Test]
    public async Task Answers_404_when_the_dashboard_is_disabled()
    {
        var context = RemoteContext();
        var middleware = new DashboardGuardMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new DevDashboardOptions { Enabled = false, AllowRemoteAccess = true }));

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
    }

    [Test]
    public void Hides_framework_noise_from_the_log_stream()
    {
        var provider = new DashboardLoggerProvider(
            new ServiceCollection().BuildServiceProvider(),
            Options.Create(new DevDashboardOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(provider.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command"), Is.SameAs(NullLogger.Instance));
            Assert.That(provider.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"), Is.SameAs(NullLogger.Instance));
            Assert.That(provider.CreateLogger("MyApp.Orders"), Is.Not.SameAs(NullLogger.Instance));
        });
    }

    private static DefaultHttpContext RemoteContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/_dashboard";
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");
        return context;
    }
}
