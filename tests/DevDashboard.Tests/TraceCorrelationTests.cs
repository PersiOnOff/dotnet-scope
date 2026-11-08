using System.Diagnostics;
using System.Net.Http.Json;
using DevDashboard.Core.Storage;

namespace DevDashboard.Tests;

public class TraceCorrelationTests
{
    [Test]
    public async Task Correlates_events_with_the_ambient_activity()
    {
        using var listener = ListenToAspNetCore();
        await using var app = await TestApp.StartAsync();

        await app.Client.GetAsync("/api/log");

        var http = app.Store.GetByType("http").Last();
        var log = app.Store.GetByType("log").Last(e => e.Message == "hello from test");

        Assert.Multiple(() =>
        {
            Assert.That(http.TraceId, Is.Not.Null.And.Not.Empty, "the request should carry the hosting trace id");
            Assert.That(log.TraceId, Is.EqualTo(http.TraceId), "a log emitted in the request belongs to the same trace");
            Assert.That(log.SpanId, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task Filters_events_by_trace_id()
    {
        using var listener = ListenToAspNetCore();
        await using var app = await TestApp.StartAsync();

        await app.Client.GetAsync("/api/log");
        await app.Client.GetAsync("/api/ping");

        var http = app.Store.GetByType("http").Last();
        var filtered = await app.Client.GetFromJsonAsync<List<TelemetryEvent>>($"/_dashboard/api/events?traceId={http.TraceId}");

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Is.Not.Empty);
            Assert.That(filtered!.All(e => e.TraceId == http.TraceId), Is.True);
        });
    }

    /// <summary>
    /// ASP.NET Core only creates its hosting <see cref="Activity"/> when something listens, which
    /// is exactly the condition an OpenTelemetry exporter creates in a real application.
    /// </summary>
    private static ActivityListener ListenToAspNetCore()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
