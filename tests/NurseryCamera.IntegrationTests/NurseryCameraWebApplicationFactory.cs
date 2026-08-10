using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Infrastructure.Persistence;

namespace NurseryCamera.IntegrationTests;

/// <summary>
/// Boots the API under the "Testing" environment with InMemory EF Core and
/// deterministic local secrets so integration tests never need SQL Server/Redis.
/// </summary>
public sealed class NurseryCameraWebApplicationFactory : WebApplicationFactory<Program>
{
    // Fixed demo AES-256 key (32 bytes, base64) — tests only, never production.
    private const string TestEncryptionKey = "oL7vZ+pusyE/SDZ+QVaXxQEayaOU84ZNzJroRz0xRLg=";
    private const string TestJwtSigningKey = "integration-test-signing-key-min-32-chars!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=NurseryCamera_Unused;",
                ["Redis:ConnectionString"] = "",
                ["Jwt:Issuer"] = "NurseryCameraSystem.Test",
                ["Jwt:Audience"] = "NurseryCameraSystem.Test.Clients",
                ["Jwt:SigningKey"] = TestJwtSigningKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "1",
                ["CameraSecurity:EncryptionKeyReference"] = TestEncryptionKey,
                ["MediaGateway:BaseUrl"] = "http://localhost:9",
                ["MediaGateway:ApiKey"] = "test-media-gateway-key",
                ["Cors:Origins:0"] = "http://localhost:4200",
                ["Cors:Origins:1"] = "http://localhost:4300",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server AppDbContext with a unique InMemory database per factory.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IAppDbContext>();

            var databaseName = $"NurseryCameraTests-{Guid.NewGuid():N}";
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        });
    }
}
