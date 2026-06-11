using Polly;
using Polly.Retry;

namespace SmartEventPlatform.EventService.Resilience;

public static class RetryPolicyFactory
{
    public static AsyncRetryPolicy CreateHttpRetryPolicy(
        ILogger logger,
        string downstreamName,
        int retryCount = 2,
        int baseDelayMilliseconds = 250)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(baseDelayMilliseconds * retryAttempt),
                onRetry: (exception, delay, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "{Downstream} call failed. Retry attempt {RetryAttempt}/{RetryCount} after {Delay} ms.",
                        downstreamName,
                        retryAttempt,
                        retryCount,
                        delay.TotalMilliseconds);
                });
    }
}