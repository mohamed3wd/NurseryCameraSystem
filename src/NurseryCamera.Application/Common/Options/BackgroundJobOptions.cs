namespace NurseryCamera.Application.Common.Options;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public int ViewingSessionExpirationIntervalSeconds { get; set; } = 10;
    public int CameraHealthCheckIntervalSeconds { get; set; } = 60;
    public int OutboxPollIntervalSeconds { get; set; } = 5;
    public int TokenCleanupIntervalMinutes { get; set; } = 30;
    public int TokenRetentionDays { get; set; } = 7;

    /// <summary>
    /// Upper bound on how many viewing sessions a single expiration pass loads. Keeps a backlog
    /// (for example after downtime) from turning one tick into an unbounded query and write.
    /// </summary>
    public int ViewingSessionExpirationBatchSize { get; set; } = 200;

    /// <summary>
    /// How long per-camera health probe results are kept. One row per camera per poll adds up
    /// quickly, and only the recent window is useful for status/uptime reporting.
    /// </summary>
    public int CameraHealthCheckRetentionDays { get; set; } = 14;
}
