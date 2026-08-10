using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NurseryCamera.Application.Abstractions.Audit;
using NurseryCamera.Application.Abstractions.Caching;
using NurseryCamera.Application.Abstractions.Identity;
using NurseryCamera.Application.Abstractions.Notifications;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Abstractions.Security;
using NurseryCamera.Application.Abstractions.Streaming;
using NurseryCamera.Application.Abstractions.Time;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Infrastructure.Audit;
using NurseryCamera.Infrastructure.BackgroundJobs;
using NurseryCamera.Infrastructure.Caching;
using NurseryCamera.Infrastructure.Identity;
using NurseryCamera.Infrastructure.Notifications;
using NurseryCamera.Infrastructure.Persistence;
using NurseryCamera.Infrastructure.Security;
using NurseryCamera.Infrastructure.Streaming;
using StackExchange.Redis;
using SystemClock = NurseryCamera.Infrastructure.Time.SystemClock;

namespace NurseryCamera.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsAndValidation(configuration);

        services.AddPersistence(configuration);
        services.AddIdentityServices();
        services.AddJwtAuthentication(configuration);
        services.AddCachingServices(configuration);

        services.AddHttpContextAccessor();

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddSingleton<ISecretEncryptionService, AesSecretEncryptionService>();
        services.AddSingleton<ITokenHashService, TokenHashService>();
        services.AddSingleton<IStreamTokenGenerator, StreamTokenGenerator>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddHttpClient("go2rtc");
        services.AddScoped<IStreamSourceResolver, StreamSourceResolver>();

        var mediaProvider = configuration.GetSection(MediaGatewayOptions.SectionName)["Provider"] ?? "Mock";
        if (string.Equals(mediaProvider, "Go2Rtc", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<ILiveStreamService, Go2RtcLiveStreamService>();
        }
        else
        {
            services.AddScoped<ILiveStreamService, MockLiveStreamService>();
        }

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        services.AddHostedService<ViewingSessionExpirationWorker>();
        services.AddHostedService<CameraHealthWorker>();
        services.AddHostedService<OutboxWorker>();
        services.AddHostedService<TokenCleanupWorker>();

        return services;
    }

    private static IServiceCollection AddOptionsAndValidation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && o.SigningKey.Length >= 32,
                "Jwt:SigningKey must be configured and at least 32 characters long.")
            .ValidateOnStart();

        services.Configure<ViewingPolicyOptions>(configuration.GetSection(ViewingPolicyOptions.SectionName));
        services.Configure<CameraSecurityOptions>(configuration.GetSection(CameraSecurityOptions.SectionName));
        services.Configure<MediaGatewayOptions>(configuration.GetSection(MediaGatewayOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<BackgroundJobOptions>(configuration.GetSection(BackgroundJobOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is not configured. Provide a SQL Server connection string.");
            }

            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // BR-023/security section 23: throttle brute-force login attempts.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwtOptions.SigningKey)
                            ? new string('0', 32)
                            : jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }

    private static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetSection(RedisOptions.SectionName)["ConnectionString"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // Dev fallback when Redis is not configured/reachable: in-process memory cache.
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        return services;
    }
}
