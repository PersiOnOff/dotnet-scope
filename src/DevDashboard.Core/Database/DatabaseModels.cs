namespace DevDashboard.Core.Database;

/// <summary>One table exposed by a data source.</summary>
/// <param name="Source">Name of the data source the table belongs to.</param>
/// <param name="Schema">Schema (empty for SQLite).</param>
/// <param name="Name">Table name.</param>
public sealed record TableInfo(string Source, string Schema, string Name);

/// <summary>A data source together with the tables it exposes.</summary>
/// <param name="Name">Data source name.</param>
/// <param name="Tables">Tables found in the database.</param>
public sealed record DataSourceInfo(string Name, IReadOnlyList<TableInfo> Tables);

/// <summary>A page of rows read from a table.</summary>
/// <param name="Columns">Column names, in select order.</param>
/// <param name="Rows">Row values, aligned with <paramref name="Columns"/>.</param>
/// <param name="Truncated">True when the table has more rows than were read.</param>
public sealed record TablePage(IReadOnlyList<string> Columns, IReadOnlyList<object?[]> Rows, bool Truncated);
