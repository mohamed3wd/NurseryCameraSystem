using System.Text.Json;
using Microsoft.Extensions.Logging;
using NurseryCamera.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace NurseryCamera.Infrastructure.Caching;

/// <summary>
/// Redis-backed cache/rate-limit/lock implementation. Redis is never the source of
/// truth for attendance, permissions, or audit data (spec section 21) - it only backs
/// ephemeral state.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    private IDatabase Database => _connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(key);
        if (!value.HasValue)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await Database.StringSetAsync(key, json, expiry, When.Always);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await Database.KeyDeleteAsync(key);
    }

    public async Task<bool> TryAcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        return await Database.StringSetAsync(key, Environment.MachineName, expiry, When.NotExists);
    }

    public async Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await Database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release distributed lock for key {Key}.", key);
        }
    }
}
