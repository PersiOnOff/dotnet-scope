using System.Net;
using System.Net.Http.Json;
using System.Text;
using DevDashboard.Core;
using DevDashboard.Core.Storage;

namespace DevDashboard.Tests;

public class HttpCaptureTests
{
    [Test]
    public async Task Captures_method_path_status_and_duration()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.Client.GetAsync("/api/ping");

        var http = app.Last("http");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(http.Method, Is.EqualTo("GET"));
            Assert.That(http.Path, Is.EqualTo("/api/ping"));
            Assert.That(http.StatusCode, Is.EqualTo(200));
            Assert.That(http.DurationMs, Is.Not.Null);
            Assert.That(http.ResponseBody, Does.Contain("ok"));
        });
    }

    [Test]
    public async Task Captures_the_request_body()
    {
        await using var app = await TestApp.StartAsync();

        var content = new StringContent("{\"title\":\"write docs\"}", Encoding.UTF8, "application/json");
        await app.Client.PostAsync("/api/echo", content);

        Assert.That(app.Last("http").RequestBody, Does.Contain("write docs"));
    }

    [Test]
    public async Task Captures_exceptions_and_marks_the_request_as_a_server_error()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.Client.GetAsync("/api/boom");

        var exception = app.Last("exception");
        var http = app.Last("http");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(exception.Message, Is.EqualTo("Boom!"));
            Assert.That(exception.Exception, Does.Contain("System.InvalidOperationException"));
            Assert.That(http.StatusCode, Is.EqualTo(500));
            Assert.That(exception.RequestId, Is.EqualTo(http.Id), "the exception should be grouped with its request");
        });
    }

    [Test]
    public async Task Groups_logs_with_the_request_that_emitted_them()
    {
        await using var app = await TestApp.StartAsync();

        await app.Client.GetAsync("/api/log");

        var http = app.Store.GetByType("http").Last();
        var log = app.Store.GetByType("log").Last(e => e.Message == "hello from test");
        Assert.That(log.RequestId, Is.EqualTo(http.Id));
    }

    [Test]
    public async Task Does_not_capture_its_own_dashboard_routes()
    {
        await using var app = await TestApp.StartAsync();

        await app.Client.GetAsync("/api/ping");
        await app.Client.GetAsync("/_dashboard/api/events");

        Assert.That(app.Store.GetByType("http").Count(e => e.Path!.StartsWith("/_dashboard")), Is.Zero);
    }

    [Test]
    public async Task Api_filters_events_by_query_and_duration()
    {
        await using var app = await TestApp.StartAsync();
        await app.Client.GetAsync("/api/ping");

        var match = await app.Client.GetFromJsonAsync<List<TelemetryEvent>>("/_dashboard/api/events?q=ping");
        var miss = await app.Client.GetFromJsonAsync<List<TelemetryEvent>>("/_dashboard/api/events?q=nothing-matches-this");
        var tooSlow = await app.Client.GetFromJsonAsync<List<TelemetryEvent>>("/_dashboard/api/events?minDurationMs=600000");

        Assert.Multiple(() =>
        {
            Assert.That(match, Is.Not.Empty);
            Assert.That(miss, Is.Empty);
            Assert.That(tooSlow, Is.Empty);
        });
    }

    [Test]
    public async Task Delete_clears_the_store()
    {
        await using var app = await TestApp.StartAsync();
        await app.Client.GetAsync("/api/ping");

        await app.Client.DeleteAsync("/_dashboard/api/events");

        Assert.That(app.Store.GetAll(), Is.Empty);
    }

    [Test]
    public async Task Respects_the_MaxEvents_option()
    {
        await using var app = await TestApp.StartAsync(options => options.MaxEvents = 2);

        for (var i = 0; i < 5; i++)
        {
            await app.Client.GetAsync("/api/ping");
        }

        Assert.That(app.Store.GetAll(), Has.Count.EqualTo(2));
    }
}
