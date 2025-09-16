using Grpc.Core;
using Polly;
using Polly.CircuitBreaker;

namespace CameraFeed.Processor.Clients;

public static class CircuitBreakerPolicyFactory
{
    public static AsyncCircuitBreakerPolicy CreateGrpcCircuitBreakerPolicy(ILogger logger, CircuitBreakerPolicyOptions? options = null)
    {
        options ??= new CircuitBreakerPolicyOptions();

        return Policy
            .Handle<RpcException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: options.ExceptionThreshold,
                durationOfBreak: options.BreakDuration,
                onBreak: (ex, breakDelay) =>
                {
                    logger.LogWarning("Circuit breaker opened for {BreakDelay} seconds due to repeated gRPC failures.", breakDelay.TotalSeconds);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset. gRPC calls will be attempted again.");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker is half-open. Testing gRPC call.");
                });
    }
}

public class CircuitBreakerPolicyOptions
{
    public int ExceptionThreshold { get; set; } = 3;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(10);
}
