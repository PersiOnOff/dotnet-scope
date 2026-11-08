using System.Data.Common;

namespace DevDashboard.Core.Database;

/// <summary>
/// A database connection the dashboard is allowed to browse. The factory keeps provider-specific
/// knowledge in the application: the package itself never references SQLite or SQL Server.
/// </summary>
public sealed class DevDashboardDataSource
{
    /// <summary>Creates a data source that opens a new connection per query.</summary>
    /// <param name="name">Name shown in the dashboard; also used in the URL.</param>
    /// <param name="connectionFactory">Creates an unopened connection. Called per request.</param>
    public DevDashboardDataSource(string name, Func<DbConnection> connectionFactory)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A data source needs a name.", nameof(name));

        Name = name;
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>Display name, also the route segment under <c>/_dashboard/api/db</c>.</summary>
    public string Name { get; }

    /// <summary>Creates an unopened connection to the database.</summary>
    public Func<DbConnection> ConnectionFactory { get; }
}
