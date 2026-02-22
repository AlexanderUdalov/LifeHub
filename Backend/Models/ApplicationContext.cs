namespace LifeHub.Models;

using Microsoft.EntityFrameworkCore;


public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<LifeFocus> LifeFocuses => Set<LifeFocus>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitDay> HabitDays => Set<HabitDay>();
    public DbSet<Addiction> Addictions => Set<Addiction>();
    public DbSet<AddictionReset> AddictionResets => Set<AddictionReset>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Keep delete behaviors explicit for consistency across the model.

        modelBuilder.Entity<User>()
            .HasMany(x => x.Tasks)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.Habits)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.Addictions)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.Goals)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.Focuses)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskItem>()
            .HasOne(x => x.Goal)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Habit>()
            .HasOne(x => x.Goal)
            .WithMany()
            .HasForeignKey(x => x.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Addiction>()
            .HasOne(x => x.Goal)
            .WithMany()
            .HasForeignKey(x => x.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AddictionReset>()
            .HasOne(x => x.Addiction)
            .WithMany(x => x.Resets)
            .HasForeignKey(x => x.AddictionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Goal>()
            .HasOne(x => x.LifeFocus)
            .WithMany(x => x.Goals)
            .HasForeignKey(x => x.LifeFocusId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HabitDay>()
            .HasIndex(x => new { x.HabitId, x.Date })
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<HabitDay>()
            .HasOne(x => x.Habit)
            .WithMany(x => x.Days)
            .HasForeignKey(x => x.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}