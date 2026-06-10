using System.Net.Http.Json;
using Polly.Retry;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.RegistrationService.Resilience;

namespace SmartEventPlatform.RegistrationService.Clients;

public sealed class EventServiceClient : IEventServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly EventServiceCircuitBreaker _circuitBreaker;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ILogger<EventServiceClient> _logger;

    public EventServiceClient(
        HttpClient httpClient,
        EventServiceCircuitBreaker circuitBreaker,
        ILogger<EventServiceClient> logger)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
        _retryPolicy = RetryPolicyFactory.CreateHttpRetryPolicy(logger, "EventService");
    }

    public Task<List<EventDto>> GetAllEventsAsync()
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync("/api/events");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
        });
    }

    public Task<EventRegistrationInfoDto> GetRegistrationInfoAsync(long eventId)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"/api/events/{eventId}/registration-info");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EventRegistrationInfoDto>();
            return result ?? throw new HttpRequestException("EventService returned empty registration info response.");
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