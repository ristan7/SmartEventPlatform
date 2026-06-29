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

    // ── Saga metode ──────────────────────────────────────────────────────────

    /// <summary>
    /// Korak 2 Sage: Pošalje POST na EventService da privremeno rezerviše mjesto.
    /// Vraća false ako nema kapaciteta (HTTP 409 Conflict).
    /// Baca iznimku pri mrežnim greškama (triggered compensation).
    /// </summary>
    public Task<bool> ReserveSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.PostAsync(
                $"/api/saga/events/{eventId}/reserve-spot?sagaId={sagaId}",
                null,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    "[Saga {SagaId}] EventService vratio 409 - nema kapaciteta za EventId={EventId}.",
                    sagaId, eventId);
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        });
    }

    /// <summary>
    /// Korak 2 kompenzacija: Briše privremenu rezervaciju iz EventService.
    /// Poziva se ako korak 3 ili 4 ne uspije.
    /// </summary>
    public Task ReleaseSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.DeleteAsync(
                $"/api/saga/events/{eventId}/release-spot?sagaId={sagaId}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Može se desiti ako kompenzacija ide dva puta - ignorišemo
                _logger.LogWarning(
                    "[Saga {SagaId}] EventService nije pronašao rezervaciju za release (EventId={EventId}). " +
                    "Moguć ponovni pokušaj kompenzacije - nastavaljamo.",
                    sagaId, eventId);
                return;
            }

            response.EnsureSuccessStatusCode();
        });
    }

    /// <summary>
    /// Korak 4 Sage: Potvrđuje rezervaciju - prebacuje je iz privremene u stvarnu evidenciju.
    /// Poziva se samo kada je saga uspješna.
    /// </summary>
    public Task ConfirmSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            using var response = await _httpClient.PostAsync(
                $"/api/saga/events/{eventId}/confirm-spot?sagaId={sagaId}",
                null,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        });
    }

    // ── Interni pomoćni metod za retry + circuit breaker ────────────────────

    private async Task ExecuteAsync(Func<Task> operation)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            _circuitBreaker.EnsureCanExecute();
            try
            {
                await operation();
                _circuitBreaker.MarkSuccess();
            }
            catch (CircuitBreakerOpenException) { throw; }
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