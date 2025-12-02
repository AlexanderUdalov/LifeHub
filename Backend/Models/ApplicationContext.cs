namespace LifeHub.Models;

using Microsoft.EntityFrameworkCore;


public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<LifeFocus> LifeFocuses { get; set; }

    public string DbPath { get; }

    public ApplicationContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "lifehub.db");
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
        options.LogTo(Console.WriteLine);
    }
}