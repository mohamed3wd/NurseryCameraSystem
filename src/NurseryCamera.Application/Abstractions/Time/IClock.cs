namespace NurseryCamera.Application.Abstractions.Time;

/// <summary>
/// Abstraction over the system clock so handlers/tests can control time deterministically.
/// All timestamps are UTC internally (BR-023).
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
