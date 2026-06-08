using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Registrations;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Models;
using SmartEventPlatform.RegistrationService.Patterns;

namespace SmartEventPlatform.RegistrationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationsController : ControllerBase
{
    private readonly RegistrationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CircuitBreaker _circuitBreaker;

    //private static int _existsForEventCounter = 0;

    public RegistrationsController(RegistrationDbContext context, IHttpClientFactory httpClientFactory, CircuitBreaker circuitBreaker)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _circuitBreaker = circuitBreaker;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegistrationDto>>> GetAll()
    {
        try
        {
            var events = await GetEventsWithCircuitBreakerAndRetryAsync();

            var registrations = await _context.Registrations
                .Include(r => r.Participant)
                .OrderBy(r => r.RegistrationDate)
                .ToListAsync();

            var result = registrations.Select(r =>
            {
                var eventDto = events.FirstOrDefault(e => e.EventId == r.EventId);

                return new RegistrationDto
                {
                    RegistrationId = r.RegistrationId,
                    RegistrationDate = r.RegistrationDate,

                    EventId = r.EventId,
                    EventName = eventDto?.EventName ?? $"Event #{r.EventId}",

                    ParticipantId = r.ParticipantId,
                    ParticipantFullName = r.Participant != null
                        ? r.Participant.FirstName + " " + r.Participant.LastName
                        : string.Empty,
                    ParticipantEmail = r.Participant?.Email ?? string.Empty
                };
            }).ToList();

            return Ok(result);
        }
        catch (CircuitBreakerOpenException)
        {
            return StatusCode(503, "EventService is temporarily unavailable. Circuit breaker is open.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "EventService timeout expired while loading registrations.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, "EventService is unavailable after retry attempts.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred while communicating with EventService.");
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<RegistrationDto>> GetById(long id)
    {
        var registration = await _context.Registrations
            .Include(r => r.Participant)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);

        if (registration == null)
        {
            return NotFound();
        }

        try
        {
            var eventInfo = await GetEventRegistrationInfoWithCircuitBreakerAndRetryAsync(registration.EventId);

            var dto = new RegistrationDto
            {
                RegistrationId = registration.RegistrationId,
                RegistrationDate = registration.RegistrationDate,

                EventId = registration.EventId,
                EventName = eventInfo.EventName,

                ParticipantId = registration.ParticipantId,
                ParticipantFullName = registration.Participant != null
                    ? registration.Participant.FirstName + " " + registration.Participant.LastName
                    : string.Empty,
                ParticipantEmail = registration.Participant?.Email ?? string.Empty
            };

            return Ok(dto);
        }
        catch (CircuitBreakerOpenException)
        {
            return StatusCode(503, "EventService is temporarily unavailable. Circuit breaker is open.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "EventService timeout expired while loading registration details.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, "EventService is unavailable after retry attempts.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred while communicating with EventService.");
        }
    }

    [HttpPost]
    public async Task<ActionResult<long>> Create(RegistrationCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var participantExists = await _context.Participants
            .AnyAsync(p => p.ParticipantId == dto.ParticipantId);

        if (!participantExists)
        {
            return BadRequest("Selected participant does not exist.");
        }

        try
        {
            var eventInfo = await GetEventRegistrationInfoWithCircuitBreakerAndRetryAsync(dto.EventId);

            if (!eventInfo.Exists)
            {
                return BadRequest("Selected event does not exist.");
            }

            var alreadyRegistered = await AlreadyRegistered(dto.EventId, dto.ParticipantId);

            if (alreadyRegistered)
            {
                return BadRequest("This participant is already registered for the selected event.");
            }

            var capacityReached = await IsEventCapacityReached(dto.EventId, eventInfo.Capacity);

            if (capacityReached)
            {
                return BadRequest("Registration is not possible because the registration location capacity has been reached.");
            }

            var registration = new Registration
            {
                EventId = dto.EventId,
                ParticipantId = dto.ParticipantId,
                RegistrationDate = dto.RegistrationDate
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = registration.RegistrationId },
                registration.RegistrationId);
        }
        catch (CircuitBreakerOpenException)
        {
            return StatusCode(503, "Registration cannot be created because EventService circuit breaker is open.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "Registration cannot be created because EventService timeout expired.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, "Registration cannot be created because EventService is unavailable after retry attempts.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred while communicating with EventService.");
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, RegistrationCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var registration = await _context.Registrations.FindAsync(id);

        if (registration == null)
        {
            return NotFound();
        }

        var participantExists = await _context.Participants
            .AnyAsync(p => p.ParticipantId == dto.ParticipantId);

        if (!participantExists)
        {
            return BadRequest("Selected participant does not exist.");
        }

        try
        {
            var eventInfo = await GetEventRegistrationInfoWithCircuitBreakerAndRetryAsync(dto.EventId);

            if (!eventInfo.Exists)
            {
                return BadRequest("Selected event does not exist.");
            }

            var duplicateRegistration = await DuplicateRegistrationExistsAsync(
                dto.EventId,
                dto.ParticipantId,
                id);

            if (duplicateRegistration)
            {
                return BadRequest("This participant is already registered for the selected event.");
            }

            var capacityReached = await IsEventCapacityReached(dto.EventId, eventInfo.Capacity, id);

            if (capacityReached)
            {
                return BadRequest("Registration is not possible because the event location capacity has been reached.");
            }

            registration.EventId = dto.EventId;
            registration.ParticipantId = dto.ParticipantId;
            registration.RegistrationDate = dto.RegistrationDate;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await RegistrationExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }
        catch (CircuitBreakerOpenException)
        {
            return StatusCode(503, "Registration cannot be updated because EventService circuit breaker is open.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "Registration cannot be updated because EventService timeout expired.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, "Registration cannot be updated because EventService is unavailable after retry attempts.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred while communicating with EventService.");
        }
    }

    [HttpGet("{id:long}/delete-info")]
    public async Task<ActionResult<RegistrationDto>> GetDeleteInfo(long id)
    {
        var registration = await _context.Registrations
            .Include(r => r.Participant)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);

        if (registration == null)
        {
            return NotFound();
        }

        try
        {
            var eventInfo = await GetEventRegistrationInfoWithCircuitBreakerAndRetryAsync(registration.EventId);

            var dto = new RegistrationDto
            {
                RegistrationId = registration.RegistrationId,
                RegistrationDate = registration.RegistrationDate,

                EventId = registration.EventId,
                EventName = eventInfo.EventName,

                ParticipantId = registration.ParticipantId,
                ParticipantFullName = registration.Participant != null
                    ? registration.Participant.FirstName + " " + registration.Participant.LastName
                    : string.Empty,
                ParticipantEmail = registration.Participant?.Email ?? string.Empty
            };

            return Ok(dto);
        }
        catch (CircuitBreakerOpenException)
        {
            return StatusCode(503, "EventService is temporarily unavailable. Circuit breaker is open.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "EventService timeout expired while loading delete information.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, "EventService is unavailable after retry attempts.");
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred while communicating with EventService.");
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var registration = await _context.Registrations.FindAsync(id);

        if (registration == null)
        {
            return NotFound();
        }

        _context.Registrations.Remove(registration);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("exists-for-event/{eventId:long}")]
    public async Task<ActionResult<bool>> ExistsForEvent(long eventId)
    {
        //_existsForEventCounter++;

        //if (_existsForEventCounter % 3 != 0)
        //{
        //    return StatusCode(500, "Simulated temporary RegistrationService error.");
        //}

        //await Task.Delay(10000);

        var exists = await _context.Registrations
            .AnyAsync(r => r.EventId == eventId);

        return Ok(exists);
    }

    private async Task<bool> RegistrationExistsAsync(long id)
    {
        return await _context.Registrations.AnyAsync(r => r.RegistrationId == id);
    }

    private async Task<bool> AlreadyRegistered(long eventId, long participantId)
    {
        return await _context.Registrations
            .AnyAsync(r => r.EventId == eventId && r.ParticipantId == participantId);
    }

    private async Task<bool> DuplicateRegistrationExistsAsync(
        long eventId,
        long participantId,
        long registrationIdToExclude)
    {
        return await _context.Registrations
            .AnyAsync(r =>
                r.RegistrationId != registrationIdToExclude &&
                r.EventId == eventId &&
                r.ParticipantId == participantId);
    }

    private async Task<bool> IsEventCapacityReached(
    long eventId,
    int capacity,
    long? registrationIdToExclude = null)
    {
        var registrationsQuery = _context.Registrations
            .Where(r => r.EventId == eventId);

        if (registrationIdToExclude.HasValue)
        {
            registrationsQuery = registrationsQuery
                .Where(r => r.RegistrationId != registrationIdToExclude.Value);
        }

        var currentRegistrationCount = await registrationsQuery.CountAsync();

        return currentRegistrationCount >= capacity;
    }

    private async Task<EventRegistrationInfoDto> GetEventRegistrationInfoWithCircuitBreakerAndRetryAsync(long eventId)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await GetEventRegistrationInfoWithPollyRetryAsync(eventId);
        });
    }

    private async Task<List<EventDto>> GetEventsWithCircuitBreakerAndRetryAsync()
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await GetEventsWithPollyRetryAsync();
        });
    }

    private async Task<EventRegistrationInfoDto> GetEventRegistrationInfoWithPollyRetryAsync(long eventId)
    {
        var eventServiceHttpClient = _httpClientFactory.CreateClient("EventService");

        HttpResponseMessage? httpResponseMessage = null;

        var retryPolicy = Polly.Policy
    .Handle<HttpRequestException>()
    .Or<TaskCanceledException>()
    .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(250));

        httpResponseMessage = await retryPolicy.ExecuteAsync<HttpResponseMessage>(async () =>
        {
            httpResponseMessage = await eventServiceHttpClient.GetAsync($"/api/events/{eventId}/registration-info");
            httpResponseMessage.EnsureSuccessStatusCode();
            return httpResponseMessage;
        });

        var eventInfo = await httpResponseMessage.Content.ReadFromJsonAsync<EventRegistrationInfoDto>();

        if (eventInfo == null)
        {
            throw new HttpRequestException("EventService returned empty registration info response.");
        }

        return eventInfo;
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