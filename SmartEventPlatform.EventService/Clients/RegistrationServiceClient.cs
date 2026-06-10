using System.Net.Http.Json;
using Polly.Retry;
using SmartEventPlatform.EventService.Resilience;

namespace SmartEventPlatform.EventService.Clients;

public sealed class RegistrationServiceClient : IRegistrationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly RegistrationServiceCircuitBreaker _circuitBreaker;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ILogger<RegistrationServiceClient> _logger;

    public RegistrationServiceClient(
        HttpClient httpClient,
        RegistrationServiceCircuitBreaker circuitBreaker,
        ILogger<RegistrationServiceClient> logger)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
        _retryPolicy = RetryPolicyFactory.CreateHttpRetryPolicy(logger, "RegistrationService");
    }

    public Task<bool> EventHasRegistrationsAsync(long eventId)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"/api/registrations/exists-for-event/{eventId}");
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
                _logger.LogWarning(ex, "RegistrationService request timed out.");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _circuitBreaker.MarkFailure();
                _logger.LogWarning(ex, "RegistrationService request failed.");
                throw;
            }
        });
    }
}