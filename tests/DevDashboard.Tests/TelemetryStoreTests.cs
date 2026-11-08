using DevDashboard.Core.Storage;

namespace DevDashboard.Tests;

public class TelemetryStoreTests
{
    [Test]
    public void Drops_the_oldest_events_when_full()
    {
        var store = new InMemoryTelemetryStore(3);

        for (var i = 0; i < 5; i++)
        {
            store.Add(new TelemetryEvent { Type = "http", Path = $"/{i}" });
        }

        Assert.Multiple(() =>
        {
            Assert.That(store.Count, Is.EqualTo(3));
            Assert.That(store.GetAll().Select(e => e.Path), Is.EqualTo(new[] { "/2", "/3", "/4" }));
        });
    }

    [Test]
    public void Filters_events_by_type_case_insensitively()
    {
        var store = new InMemoryTelemetryStore();
        store.Add(new TelemetryEvent { Type = "http" });
        store.Add(new TelemetryEvent { Type = "sql" });
        store.Add(new TelemetryEvent { Type = "SQL" });

        Assert.That(store.GetByType("sql"), Has.Count.EqualTo(2));
        Assert.That(store.GetByType("http"), Has.Count.EqualTo(1));
    }

    [Test]
    public void Clear_removes_everything()
    {
        var store = new InMemoryTelemetryStore();
        store.Add(new TelemetryEvent { Type = "log" });
        store.Clear();

        Assert.That(store.GetAll(), Is.Empty);
    }

    [Test]
    public void Rejects_a_non_positive_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryTelemetryStore(0));
    }
}
