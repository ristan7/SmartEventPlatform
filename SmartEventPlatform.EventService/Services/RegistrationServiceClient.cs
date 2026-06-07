using System.Net.Http.Json;
using SmartEventPlatform.EventService.Patterns;

namespace SmartEventPlatform.EventService.Services;

public class RegistrationServiceClient : IRegistrationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILogger<RegistrationServiceClient> _logger;

    public RegistrationServiceClient(
        HttpClient httpClient,
        CircuitBreaker circuitBreaker,
        ILogger<RegistrationServiceClient> logger)
    {
        _httpClient = httpClient;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    public async Task<bool> EventHasRegistrationsAsync(long eventId)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(() =>
                GetEventHasRegistrationsWithManualRetryAsync(eventId));
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogWarning(ex,
                "Circuit breaker is open while checking registrations for event {EventId}. Deletion will be blocked.",
                eventId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RegistrationService is unavailable while checking registrations for event {EventId}. Deletion will be blocked.",
                eventId);

            return true;
        }
    }

    private async Task<bool> GetEventHasRegistrationsWithManualRetryAsync(long eventId)
    {
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Calling RegistrationService to check registrations for event {EventId}. Attempt {Attempt}/{MaxRetries}.",
                    eventId,
                    attempt,
                    maxRetries);

                var result = await _httpClient.GetFromJsonAsync<bool>(
                    $"api/registrations/exists-for-event/{eventId}");

                return result;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "Temporary failure while calling RegistrationService. Retrying attempt {NextAttempt}/{MaxRetries}.",
                    attempt + 1,
                    maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        _logger.LogWarning(
            "RegistrationService call failed after {MaxRetries} attempts for event {EventId}.",
            maxRetries,
            eventId);

        throw new HttpRequestException(
            "RegistrationService did not respond after retry attempts.");
    }
}