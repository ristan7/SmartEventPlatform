using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Events.Integration;
using SmartEventPlatform.Contracts.Registrations;
using SmartEventPlatform.RegistrationService.Clients;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Messaging;
using SmartEventPlatform.RegistrationService.Models;
using System.Text.Json;

namespace SmartEventPlatform.RegistrationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationsController : ControllerBase
{
    private readonly RegistrationDbContext _context;
    private readonly IEventServiceClient _eventServiceClient;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(RegistrationDbContext context, IEventServiceClient eventServiceClient, ILogger<RegistrationsController> logger)
    {
        _context = context;
        _eventServiceClient = eventServiceClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegistrationDto>>> GetAll()
    {

        var events = await _eventServiceClient.GetAllEventsAsync();

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


        var eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(registration.EventId);

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

        var eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(dto.EventId);

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

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var registration = new Registration
            {
                EventId = dto.EventId,
                ParticipantId = dto.ParticipantId,
                RegistrationDate = dto.RegistrationDate
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            // Outbox — u istoj transakciji
            _context.OutboxMessages.Add(new OutboxMessage
            {
                EventType = nameof(RegistrationCreatedEvent),
                Payload = JsonSerializer.Serialize(new RegistrationCreatedEvent
                {
                    RegistrationId = registration.RegistrationId,
                    EventId = registration.EventId,
                    ParticipantId = registration.ParticipantId,
                    RegistrationDate = registration.RegistrationDate
                }),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = registration.RegistrationId }, registration.RegistrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greska pri kreiranju registracije.");
            await transaction.RollbackAsync();
            throw;
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

        var eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(dto.EventId);

        if (!eventInfo.Exists)
        {
            return BadRequest("Selected event does not exist.");
        }

        var duplicateRegistration = await DuplicateRegistrationExistsAsync(dto.EventId, dto.ParticipantId, id);

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


        var eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(registration.EventId);

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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var registration = await _context.Registrations.FindAsync(id);

        if (registration == null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Outbox — u istoj transakciji kao i brisanje
            _context.OutboxMessages.Add(new OutboxMessage
            {
                EventType = nameof(RegistrationDeletedEvent),
                Payload = JsonSerializer.Serialize(new RegistrationDeletedEvent
                {
                    RegistrationId = registration.RegistrationId,
                    EventId = registration.EventId
                }),
                CreatedAt = DateTime.UtcNow
            });

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greska pri brisanju registracije.");
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpGet("exists-for-event/{eventId:long}")]
    public async Task<ActionResult<bool>> ExistsForEvent(long eventId)
    {
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

    private async Task<bool> DuplicateRegistrationExistsAsync(long eventId, long participantId, long registrationIdToExclude)
    {
        return await _context.Registrations
            .AnyAsync(r => r.RegistrationId != registrationIdToExclude && r.EventId == eventId && r.ParticipantId == participantId);
    }

    private async Task<bool> IsEventCapacityReached(long eventId, int capacity, long? registrationIdToExclude = null)
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
}