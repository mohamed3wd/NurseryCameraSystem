namespace NurseryCamera.Application.Common.Options;

/// <summary>
/// Bound from the "ViewingPolicy" configuration section. See spec section 13.
/// These values must never be hardcoded in handlers.
/// </summary>
public sealed class ViewingPolicyOptions
{
    public const string SectionName = "ViewingPolicy";

    public int MaxSessionDurationMinutes { get; set; } = 15;
    public int TokenLifetimeSeconds { get; set; } = 60;
    public int IdleTimeoutSeconds { get; set; } = 120;
    public int MaxConcurrentSessionsPerParent { get; set; } = 1;

    /// <summary>0 (or less) means unlimited concurrent sessions per child.</summary>
    public int MaxConcurrentSessionsPerChild { get; set; } = 0;
}
