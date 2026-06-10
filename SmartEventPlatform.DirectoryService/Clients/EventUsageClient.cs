using System.Net.Http.Json;
using Polly.Retry;
using SmartEventPlatform.DirectoryService.Resilience;

namespace SmartEventPlatform.DirectoryService.Clients;

public sealed class EventUsageClient : IEventUsageClient
{
    private readonly HttpClient _httpClient;
    private readonly EventServiceCircuitBreaker _circuitBreaker;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ILogger<EventUsageClient> _logger;

    public EventUsageClient(
        HttpClient httpClient,
        EventServiceCircuitBreaker circuitBreaker,
        ILogger<EventUsageClient> logger)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
        _retryPolicy = RetryPolicyFactory.CreateHttpRetryPolicy(logger, "EventService");
    }

    public Task<bool> ExistsForLocationAsync(long locationId)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"/api/events/exists-for-location/{locationId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        });
    }

    public Task<bool> ExistsForSpeakerAsync(long speakerId)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"/api/eventspeakers/exists-for-speaker/{speakerId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        });
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            _circuitBreaker.EnsureCanExecute();

            try
            {
                var result = await operation();
                _circuitBreaker.MarkSuccess();
                return result;
            }
            catch (CircuitBreakerOpenException)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _circuitBreaker.MarkFailure();
                _logger.LogWarning(ex, "EventService request timed out.");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _circuitBreaker.MarkFailure();
                _logger.LogWarning(ex, "EventService request failed.");
                throw;
            }
        });
    }
}