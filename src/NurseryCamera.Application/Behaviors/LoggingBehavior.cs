using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NurseryCamera.Application.Behaviors;

/// <summary>Structured logging for every request that flows through MediatR (spec section 36).</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Above this, a successful request is worth surfacing at Information.</summary>
    private const long SlowRequestThresholdMs = 500;

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);

            // Routine successes are Debug so a busy nursery doesn't pay for (or drown in) one
            // Information entry per request; only slow ones stay visible at default levels.
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            if (elapsedMs >= SlowRequestThresholdMs)
            {
                _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, elapsedMs);
            }
            else
            {
                _logger.LogDebug("Handled {RequestName} in {ElapsedMs}ms", requestName, elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed {RequestName} after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
