using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using DevDashboard.Core.Database;
using Microsoft.Data.Sqlite;

namespace DevDashboard.Tests;

public class DatabaseTabTests
{
    [Test]
    public async Task Lists_tables_of_a_registered_source()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var app = await TestApp.StartAsync(options => options.AddDataSource("test", database.CreateConnection));

        var sources = await app.Client.GetFromJsonAsync<List<DataSourceInfo>>("/_dashboard/api/db");

        Assert.Multiple(() =>
        {
            Assert.That(sources, Has.Count.EqualTo(1));
            Assert.That(sources![0].Name, Is.EqualTo("test"));
            Assert.That(sources[0].Tables.Select(table => table.Name), Does.Contain("Widgets"));
        });
    }

    [Test]
    public async Task Reads_rows_of_a_table()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var app = await TestApp.StartAsync(options => options.AddDataSource("test", database.CreateConnection));

        var page = await app.Client.GetFromJsonAsync<TablePage>("/_dashboard/api/db/test/Widgets");

        Assert.Multiple(() =>
        {
            Assert.That(page, Is.Not.Null);
            Assert.That(page!.Columns, Is.EqualTo(new[] { "Id", "Name" }));
            Assert.That(page.Rows, Has.Count.EqualTo(2));
            Assert.That(page.Rows[0][1]?.ToString(), Is.EqualTo("alpha"));
            Assert.That(page.Truncated, Is.False);
        });
    }

    [Test]
    public async Task Reports_truncation_when_the_limit_is_reached()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var app = await TestApp.StartAsync(options => options.AddDataSource("test", database.CreateConnection));

        var page = await app.Client.GetFromJsonAsync<TablePage>("/_dashboard/api/db/test/Widgets?limit=1");

        Assert.Multiple(() =>
        {
            Assert.That(page!.Rows, Has.Count.EqualTo(1));
            Assert.That(page.Truncated, Is.True);
        });
    }

    [Test]
    public async Task Is_404_for_an_unknown_table()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var app = await TestApp.StartAsync(options => options.AddDataSource("test", database.CreateConnection));

        var response = await app.Client.GetAsync("/_dashboard/api/db/test/Secrets");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Is_404_for_an_unknown_source()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var app = await TestApp.StartAsync(options => options.AddDataSource("test", database.CreateConnection));

        var response = await app.Client.GetAsync("/_dashboard/api/db/other/Widgets");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Returns_an_empty_list_when_no_source_is_registered()
    {
        await using var app = await TestApp.StartAsync();

        var sources = await app.Client.GetFromJsonAsync<List<DataSourceInfo>>("/_dashboard/api/db");

        Assert.That(sources, Is.Empty);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private TestDatabase(string path) => _path = path;

        public DbConnection CreateConnection() =>
            new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = _path,
                Pooling = false,
            }.ToString());

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"dotnetscope-{Guid.NewGuid():N}.db");
            var database = new TestDatabase(path);

            await using var connection = database.CreateConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE Widgets (
                    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );
                INSERT INTO Widgets (Name) VALUES ('alpha'), ('beta');
                """;
            await command.ExecuteNonQueryAsync();

            return database;
        }

        public ValueTask DisposeAsync()
        {
            File.Delete(_path);
            return ValueTask.CompletedTask;
        }
    }
}
