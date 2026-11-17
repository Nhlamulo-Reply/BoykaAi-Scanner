using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Boyka.Infrastructure.Data;

/// <summary>
/// Design-time factory used by EF Core tooling (migrations, scaffolding).
/// Reads the connection string from appsettings.json in Boyka.API.
/// </summary>
public class BoykaDbContextFactory : IDesignTimeDbContextFactory<BoykaDbContext>
{
    public BoykaDbContext CreateDbContext(string[] args)
    {
        // Walk up from the Infrastructure project output dir to find the API project
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Boyka.API");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("BoykaDb")
            ?? throw new InvalidOperationException("Connection string 'BoykaDb' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<BoykaDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BoykaDbContext(optionsBuilder.Options);
    }
}
