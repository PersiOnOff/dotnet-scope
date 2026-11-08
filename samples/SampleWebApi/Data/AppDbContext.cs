using Microsoft.EntityFrameworkCore;
namespace SampleWebApi.Data;
public class Todo { public int Id { get; set; } public string Title { get; set; } = ""; public bool Completed { get; set; } }
public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }
    public DbSet<Todo> Todos => Set<Todo>();
}
