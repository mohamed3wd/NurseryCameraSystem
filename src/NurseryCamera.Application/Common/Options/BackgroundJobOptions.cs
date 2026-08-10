namespace NurseryCamera.Application.Common.Options;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public int ViewingSessionExpirationIntervalSeconds { get; set; } = 10;
    public int CameraHealthCheckIntervalSeconds { get; set; } = 60;
    public int OutboxPollIntervalSeconds { get; set; } = 5;
    public int TokenCleanupIntervalMinutes { get; set; } = 30;
    public int TokenRetentionDays { get; set; } = 7;
}
