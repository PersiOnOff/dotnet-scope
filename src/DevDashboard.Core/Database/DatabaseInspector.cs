using System.Data;
using System.Data.Common;
using System.Globalization;

namespace DevDashboard.Core.Database;

/// <summary>
/// Reads table metadata and rows through plain ADO.NET, so any provider that implements
/// <see cref="DbConnection.GetSchema(string)"/> works without a reference to it.
/// </summary>
internal static class DatabaseInspector
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);
    private const int MaxCellChars = 500;

    public static async Task<IReadOnlyList<DataSourceInfo>> DescribeAsync(
        IReadOnlyList<DevDashboardDataSource> sources,
        CancellationToken cancellationToken)
    {
        var result = new List<DataSourceInfo>(sources.Count);
        foreach (var source in sources)
        {
            result.Add(new DataSourceInfo(source.Name, await GetTablesAsync(source, cancellationToken).ConfigureAwait(false)));
        }

        return result;
    }

    public static async Task<IReadOnlyList<TableInfo>> GetTablesAsync(
        DevDashboardDataSource source,
        CancellationToken cancellationToken)
    {
        using var timeout = CreateTimeout(cancellationToken);
        await using var connection = source.ConnectionFactory();
        await connection.OpenAsync(timeout.Token).ConfigureAwait(false);
        return ReadTables(connection, source.Name);
    }

    public static async Task<TablePage> ReadTableAsync(
        DevDashboardDataSource source,
        string table,
        int limit,
        CancellationToken cancellationToken)
    {
        using var timeout = CreateTimeout(cancellationToken);
        await using var connection = source.ConnectionFactory();
        await connection.OpenAsync(timeout.Token).ConfigureAwait(false);

        var match = ReadTables(connection, source.Name)
            .FirstOrDefault(candidate => string.Equals(candidate.Name, table, StringComparison.OrdinalIgnoreCase));

        if (match is null) throw new TableNotFoundException(table);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {Quote(connection, match.Name)}";
        command.CommandTimeout = (int)CommandTimeout.TotalSeconds;

        // Read one row past the limit so "there is more" is known without a COUNT(*).
        await using var reader = await command.ExecuteReaderAsync(timeout.Token).ConfigureAwait(false);

        var columns = new string[reader.FieldCount];
        for (var index = 0; index < reader.FieldCount; index++) columns[index] = reader.GetName(index);

        var rows = new List<object?[]>(Math.Min(limit, 64));
        var truncated = false;
        while (await reader.ReadAsync(timeout.Token).ConfigureAwait(false))
        {
            if (rows.Count == limit)
            {
                truncated = true;
                break;
            }

            var values = new object?[reader.FieldCount];
            for (var index = 0; index < reader.FieldCount; index++)
            {
                values[index] = reader.IsDBNull(index) ? null : Normalize(reader.GetValue(index));
            }

            rows.Add(values);
        }

        return new TablePage(columns, rows, truncated);
    }

    private static IReadOnlyList<TableInfo> ReadTables(DbConnection connection, string source)
    {
        var provider = connection.GetType().FullName ?? "";

        // GetSchema("Tables") is not implemented by every provider (Microsoft.Data.Sqlite
        // returns an empty collection), so the two providers we know are queried directly and
        // everything else falls back to the standard metadata call.
        if (provider.Contains("Sqlite", StringComparison.Ordinal))
        {
            return QueryNames(
                connection,
                source,
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name",
                schemaColumn: null);
        }

        if (provider.Contains("SqlClient", StringComparison.Ordinal))
        {
            return QueryNames(
                connection,
                source,
                "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME",
                schemaColumn: 0);
        }

        return FromSchema(connection, source);
    }

    private static IReadOnlyList<TableInfo> QueryNames(
        DbConnection connection,
        string source,
        string sql,
        int? schemaColumn)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = (int)CommandTimeout.TotalSeconds;

            using var reader = command.ExecuteReader();
            var tables = new List<TableInfo>();
            while (reader.Read())
            {
                var schema = schemaColumn is null ? "" : reader.GetString(schemaColumn.Value);
                var name = reader.GetString(schemaColumn is null ? 0 : 1);
                if (string.IsNullOrWhiteSpace(name)) continue;
                tables.Add(new TableInfo(source, schema, name));
            }

            return tables;
        }
        catch (Exception)
        {
            return Array.Empty<TableInfo>();
        }
    }

    private static IReadOnlyList<TableInfo> FromSchema(DbConnection connection, string source)
    {
        DataTable schema;
        try
        {
            schema = connection.GetSchema("Tables");
        }
        catch (Exception)
        {
            return Array.Empty<TableInfo>();
        }

        var tables = new List<TableInfo>();
        foreach (DataRow row in schema.Rows)
        {
            var name = Text(row, "TABLE_NAME");
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase)) continue;

            var type = Text(row, "TABLE_TYPE");
            if (type is not null && type.Contains("VIEW", StringComparison.OrdinalIgnoreCase)) continue;

            tables.Add(new TableInfo(source, Text(row, "TABLE_SCHEMA") ?? "", name));
        }

        return tables;
    }

    /// <summary>Quote an identifier for the provider. The name is validated against the schema first.</summary>
    private static string Quote(DbConnection connection, string identifier)
    {
        var provider = connection.GetType().FullName ?? "";
        return provider.Contains("SqlClient", StringComparison.Ordinal)
            ? "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]"
            : "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static object? Normalize(object value) => value switch
    {
        null or DBNull => null,
        byte[] bytes => bytes.Length == 0
            ? ""
            : Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, 32)) + (bytes.Length > 32 ? "…" : ""),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset offset => offset.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan span => span.ToString("c", CultureInfo.InvariantCulture),
        string text => text.Length > MaxCellChars ? text[..MaxCellChars] + "…" : text,
        _ => value,
    };

    private static string? Text(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column)) return null;
        var value = row[column];
        return value is null or DBNull ? null : value.ToString();
    }

    private static CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(CommandTimeout);
        return source;
    }
}

/// <summary>Thrown when a requested table is not part of the source's schema.</summary>
internal sealed class TableNotFoundException : Exception
{
    public TableNotFoundException(string table) : base($"Table '{table}' does not exist.") => Table = table;

    public string Table { get; }
}
