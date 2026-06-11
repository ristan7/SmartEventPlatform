using System.Net;
using System.Net.Http.Json;
using Polly.Retry;
using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatform.EventService.Resilience;

namespace SmartEventPlatform.EventService.Clients;

public sealed class DirectoryServiceClient : IDirectoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly DirectoryServiceCircuitBreaker _circuitBreaker;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ILogger<DirectoryServiceClient> _logger;

    public DirectoryServiceClient(
        HttpClient httpClient,
        DirectoryServiceCircuitBreaker circuitBreaker,
        ILogger<DirectoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
        _retryPolicy = RetryPolicyFactory.CreateHttpRetryPolicy(logger, "DirectoryService");
    }

    public Task<LocationDto?> GetLocationAsync(long locationId)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"/api/locations/{locationId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LocationDto>();
        });
    }

    public Task<SpeakerDto?> GetSpeakerAsync(long speakerId)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"/api/speakers/{speakerId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SpeakerDto>();
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
                _logger.LogWarning(ex, "DirectoryService request timed out.");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _circuitBreaker.MarkFailure();
                _logger.LogWarning(ex, "DirectoryService request failed.");
                throw;
            }
        });
    }
}