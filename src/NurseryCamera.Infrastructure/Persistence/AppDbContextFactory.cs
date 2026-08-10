using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NurseryCamera.Infrastructure.Persistence;

/// <summary>
/// Enables `dotnet ef migrations` tooling to construct <see cref="AppDbContext"/> at design
/// time without requiring the API host to be fully wired up. Never used at runtime - the real
/// DbContext is configured via <c>AddInfrastructure</c> using the actual application configuration.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NURSERYCAMERA_DESIGNTIME_CONNECTION")
            ?? "Server=localhost;Database=NurseryCameraDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }
}
