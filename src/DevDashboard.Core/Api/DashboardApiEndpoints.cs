using DevDashboard.Core.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DevDashboard.Core.Api;

/// <summary>JSON API used by the dashboard UI.</summary>
public static class DashboardApiEndpoints
{
    /// <summary>Maps <c>/_dashboard/api</c>.</summary>
    public static void MapDashboardApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(DevDashboardDefaults.ApiPath);

        group.MapGet("/events", (
            ITelemetryStore store,
            string? type = null,
            string? q = null,
            long? minDurationMs = null,
            int? limit = null) => Results.Ok(Query(store.GetAll(), type, q, minDurationMs, limit)));

        group.MapGet("/events/{type}", (string type, ITelemetryStore store) =>
            Results.Ok(store.GetByType(type)));

        // One request plus everything it caused: the unit you want to paste into an issue.
        group.MapGet("/requests/{id:guid}", (Guid id, ITelemetryStore store) => Bundle(id, store));

        group.MapDelete("/events", (ITelemetryStore store) =>
        {
            store.Clear();
            return Results.NoContent();
        });

        group.MapGet("/info", (ITelemetryStore store) => Results.Ok(new
        {
            name = "DotNetScope",
            version = typeof(DashboardApiEndpoints).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            events = store.GetAll().Count,
        }));
    }

    private static IResult Bundle(Guid id, ITelemetryStore store)
    {
        var events = store.GetAll();
        var request = events.FirstOrDefault(e => e.Id == id && e.Type == "http");
        if (request is null) return Results.NotFound();

        var related = events
            .Where(e => e.RequestId == id && e.Id != id)
            .ToList();

        return Results.Ok(new RequestBundle(request, related));
    }

    internal static IReadOnlyList<TelemetryEvent> Query(
        IReadOnlyList<TelemetryEvent> events,
        string? type,
        string? q,
        long? minDurationMs,
        int? limit)
    {
        IEnumerable<TelemetryEvent> query = events;

        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => string.Equals(e.Type, type, StringComparison.OrdinalIgnoreCase));
        }

        if (minDurationMs is > 0)
        {
            query = query.Where(e => (e.DurationMs ?? 0) >= minDurationMs.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(e => Matches(e, term));
        }

        var result = query.ToList();
        if (limit is > 0 && result.Count > limit.Value)
        {
            result = result.TakeLast(limit.Value).ToList();
        }

        return result;
    }

    private static bool Matches(TelemetryEvent e, string term) =>
        Contains(e.Path, term) ||
        Contains(e.Method, term) ||
        Contains(e.Message, term) ||
        Contains(e.Sql, term) ||
        Contains(e.Category, term) ||
        Contains(e.LogLevel, term) ||
        Contains(e.Exception, term) ||
        (e.StatusCode?.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool Contains(string? value, string term) =>
        value?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
}
