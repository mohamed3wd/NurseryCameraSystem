namespace NurseryCamera.Application.Abstractions.Caching;

/// <summary>
/// Redis-backed cache abstraction (spec section 21). SQL Server remains the source of truth;
/// this is only for rate limiting, short-lived locks, presence, and authorization caching.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Attempts to acquire a short-lived distributed lock. Returns true if acquired.</summary>
    Task<bool> TryAcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default);
}
