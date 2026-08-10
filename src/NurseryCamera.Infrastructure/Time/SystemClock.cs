using NurseryCamera.Application.Abstractions.Time;

namespace NurseryCamera.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
