using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using NurseryCamera.Application.Abstractions.Caching;

namespace NurseryCamera.Infrastructure.Caching;

/// <summary>
/// In-process fallback used for local development when Redis is unavailable.
/// Not suitable for multi-instance deployments (locks are process-local).
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _locks = new();

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry.Value;
        }

        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var acquired = _locks.TryAdd(key, 0);
        if (!acquired)
        {
            return Task.FromResult(false);
        }

        _memoryCache.Set(key, true, expiry);
        _ = Task.Delay(expiry, cancellationToken).ContinueWith(
            completedTask => _locks.TryRemove(key, out _),
            TaskScheduler.Default);

        return Task.FromResult(true);
    }

    public Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
    {
        _locks.TryRemove(key, out _);
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}
