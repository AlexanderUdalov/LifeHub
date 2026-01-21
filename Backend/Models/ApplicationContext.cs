namespace LifeHub.Models;

using Microsoft.EntityFrameworkCore;


public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<LifeFocus> LifeFocuses => Set<LifeFocus>();
}