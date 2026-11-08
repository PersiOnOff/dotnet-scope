using DevDashboard.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using SampleWebApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Console only: the Windows Event Log provider is not available in every environment and its
// failures would otherwise show up in the dashboard as exceptions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddDevDashboard(options =>
{
    // Allow other machines/containers to reach the dashboard. Off by default on purpose.
    options.AllowRemoteAccess = false;
    options.MaxEvents = 1000;
});

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=sample.db"));

var app = builder.Build();

app.UseDevDashboard();

app.MapGet("/", () => "Hello. The dashboard lives at /_dashboard");

app.MapGet("/api/hello", (ILogger<Program> log) =>
{
    log.LogInformation("Hello at {Time}", DateTime.UtcNow);
    return new { Ok = true };
});

app.MapGet("/api/slow", async () =>
{
    await Task.Delay(300);
    return new { Slow = true };
});

app.MapGet("/api/error", () =>
{
    throw new InvalidOperationException("Boom!");
});

app.MapGet("/api/todos", async (AppDbContext db) => await db.Todos.OrderBy(t => t.Id).ToListAsync());

app.MapPost("/api/todos", async (AppDbContext db, string title) =>
{
    var todo = new Todo { Title = title };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.MapPost("/api/todos/{id:int}/complete", async (int id, AppDbContext db, ILogger<Program> log) =>
{
    var todo = await db.Todos.FindAsync(id)
        ?? throw new InvalidOperationException($"Todo {id} does not exist.");

    todo.Completed = true;
    await db.SaveChangesAsync();
    log.LogInformation("Completed todo {Id}: {Title}", todo.Id, todo.Title);
    return Results.Ok(todo);
});

app.MapGet("/api/checkout", async (AppDbContext db, ILogger<Program> log) =>
{
    log.LogInformation("Checkout started");
    var count = await db.Todos.CountAsync();
    await Task.Delay(150);
    log.LogWarning("Checkout rejected after {Count} items", count);
    throw new InvalidOperationException("Payment provider returned 402");
});

app.UseDevDashboardEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Todos.Any())
    {
        db.Todos.AddRange(
            new Todo { Title = "Read the README" },
            new Todo { Title = "Star the repo ⭐" },
            new Todo { Title = "Open /_dashboard" });
        db.SaveChanges();
    }
}

app.Run();
