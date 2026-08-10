using Microsoft.EntityFrameworkCore;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.UnitTests.Helpers;

internal static class InMemoryDbFactory
{
    public static AppDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
