using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CameraFeed.Processor.Clients;

public static class CircuitBreakerFactory
{
    public static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy<T>(string type, ILogger logger, CircuitBreakerOptions? options = null) where T : Exception
    {
        options ??= new CircuitBreakerOptions();

        return Policy
            .Handle<T>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: options.ExceptionThreshold,
                durationOfBreak: options.BreakDuration,
                onBreak: (ex, breakDelay) =>
                {
                    logger.LogWarning("Circuit breaker opened for {BreakDelay} seconds due to repeated {type} failures.", breakDelay.TotalSeconds, type);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset. {type} calls will be attempted again.", type);
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker is half-open. Testing {type} call.", type);
                });
    }

    public static AsyncRetryPolicy CreateRestartPolicy<T>(string camName, string type, ILogger logger, RestartOptions? options = null) where T : Exception
    {
        options ??= new RestartOptions();

        return Policy
            .Handle<T>()
            .WaitAndRetryForeverAsync(
                sleepDurationProvider: _ => options.BreakDuration,
                onRetry: (ex, breakDelay) =>
                {
                    logger.LogWarning("Retrying {type} connection for {camName} after failure to start.", type, camName);
                });
    }
}

public class CircuitBreakerOptions
{
    public int ExceptionThreshold { get; set; } = 3;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(10);
}

public class RestartOptions
{
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(10);
}
