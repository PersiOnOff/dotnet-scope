using DevDashboard.Core.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace DevDashboard.Core.Api;

/// <summary>Read-only browsing of the data sources registered on <see cref="DevDashboardOptions"/>.</summary>
public static class DatabaseApiEndpoints
{
    /// <summary>Maps <c>/_dashboard/api/db</c>.</summary>
    public static void MapDatabaseApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(DevDashboardDefaults.ApiPath);

        group.MapGet("/db", (IOptions<DevDashboardOptions> options, CancellationToken cancellationToken) =>
            ListAsync(options.Value.DataSources.ToArray(), cancellationToken));

        group.MapGet("/db/{source}/{table}", (
            string source,
            string table,
            int? limit,
            IOptions<DevDashboardOptions> options,
            CancellationToken cancellationToken) =>
            ReadAsync(options.Value.DataSources, source, table, Math.Clamp(limit ?? 50, 1, 500), cancellationToken));
    }

    private static async Task<IResult> ListAsync(
        IReadOnlyList<DevDashboardDataSource> sources,
        CancellationToken cancellationToken)
    {
        if (sources.Count == 0) return Results.Ok(Array.Empty<DataSourceInfo>());

        try
        {
            return Results.Ok(await DatabaseInspector.DescribeAsync(sources, cancellationToken));
        }
        catch (Exception exception)
        {
            return Results.Problem(detail: exception.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> ReadAsync(
        IList<DevDashboardDataSource> sources,
        string source,
        string table,
        int limit,
        CancellationToken cancellationToken)
    {
        var dataSource = sources.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, source, StringComparison.OrdinalIgnoreCase));

        if (dataSource is null)
        {
            return Results.NotFound(new { error = $"Unknown data source '{source}'." });
        }

        try
        {
            return Results.Ok(await DatabaseInspector.ReadTableAsync(dataSource, table, limit, cancellationToken));
        }
        catch (TableNotFoundException exception)
        {
            return Results.NotFound(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            return Results.Problem(detail: exception.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
