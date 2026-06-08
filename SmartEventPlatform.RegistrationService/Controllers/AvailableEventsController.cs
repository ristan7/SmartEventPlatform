using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Patterns;

namespace SmartEventPlatform.RegistrationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailableEventsController : ControllerBase
{
    private readonly RegistrationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CircuitBreaker _circuitBreaker;

    public AvailableEventsController(RegistrationDbContext context, IHttpClientFactory httpClientFactory, CircuitBreaker circuitBreaker)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _circuitBreaker = circuitBreaker;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvailableEventDto>>> GetAvailableEvents()
    {
        try
        {
            var events = await GetEventsWithCircuitBreakerAndRetryAsync();

            var now = DateTime.Now;

            var futureEvents = events
                .Where(e => e.EventDateTime >= now)
                .ToList();

            var eventIds = futureEvents
                .Select(e => e.EventId)
                .ToList();

            var registrationCounts = await _context.Registrations
                .Where(r => eventIds.Contains(r.EventId))
                .GroupBy(r => r.EventId)
                .Select(g => new
                {
                    EventId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var availableEvents = futureEvents
                .Select(e =>
                {
                    var registeredCount = registrationCounts
                        .FirstOrDefault(rc => rc.EventId == e.EventId)?.Count ?? 0;

                    return new AvailableEventDto
                    {
                        EventId = e.EventId,
                        EventName = e.EventName,
                        Agenda = e.Agenda,
                        EventDateTime = e.EventDateTime,
                        DurationInMinutes = e.DurationInMinutes,
                        RegistrationFee = e.RegistrationFee,
                        LocationName = e.LocationName,
                        Capacity = e.Capacity,
                        RegisteredCount = registeredCount,
                        Speakers = e.Speakers
                    };
                })
                .Where(e => e.RegisteredCount < e.Capacity)
                .OrderBy(e => e.EventDateTime)
                .ToList();

            return Ok(availableEvents);
        }
        catch (CircuitBreakerOpenException)
        {
            return StatusCode(503, "Available events cannot be loaded because EventService circuit breaker is open.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "Available events cannot be loaded because EventService timeout expired.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, "Available events cannot be loaded because EventService is unavailable after retry attempts.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred while communicating with EventService.");
        }
    }

    private async Task<List<EventDto>> GetEventsWithCircuitBreakerAndRetryAsync()
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await GetEventsWithPollyRetryAsync();
        });
    }

    private async Task<List<EventDto>> GetEventsWithPollyRetryAsync()
    {
        var eventServiceHttpClient = _httpClientFactory.CreateClient("EventService");

        HttpResponseMessage? httpResponseMessage = null;

        var retryPolicy = Polly.Policy
    .Handle<HttpRequestException>()
    .Or<TaskCanceledException>()
    .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(250));

        httpResponseMessage = await retryPolicy.ExecuteAsync<HttpResponseMessage>(async () =>
        {
            httpResponseMessage = await eventServiceHttpClient.GetAsync("/api/events");
            httpResponseMessage.EnsureSuccessStatusCode();
            return httpResponseMessage;
        });

        var events = await httpResponseMessage.Content.ReadFromJsonAsync<List<EventDto>>();

        return events ?? new List<EventDto>();
    }
}