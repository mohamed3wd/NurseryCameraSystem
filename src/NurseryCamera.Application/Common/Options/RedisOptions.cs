namespace NurseryCamera.Application.Common.Options;

/// <summary>
/// Bound from the "Redis" configuration section. See spec section 21.
/// Redis is used for caching/rate limiting/presence - never as the source of truth
/// for attendance, parent-child relationships, camera permissions, or audit logs.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "nursery-camera:";
}
