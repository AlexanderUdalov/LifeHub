namespace LifeHub.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
{
    public ApplicationContext CreateDbContext(string[] args)
    {
        var dbPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "database", "lifehub.db")
        );

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new ApplicationContext(optionsBuilder.Options);
    }
}
