using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.Contracts.Registrations;
using SmartEventPlatform.RegistrationService.Clients;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Messaging;
using SmartEventPlatform.RegistrationService.Models;
using SmartEventPlatform.RegistrationService.Saga;
using System.Text.Json;

namespace SmartEventPlatform.RegistrationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationsController : ControllerBase
{
    private readonly RegistrationDbContext _context;
    private readonly IEventServiceClient _eventServiceClient;
    private readonly IRabbitMqEventQueryClient _eventQueryClient;
    private readonly IEmailQueuePublisher _emailQueuePublisher;
    private readonly RegistrationSagaOrchestrator _sagaOrchestrator;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(
        RegistrationDbContext context,
        IEventServiceClient eventServiceClient,
        IRabbitMqEventQueryClient eventQueryClient,
        IEmailQueuePublisher emailQueuePublisher,
        RegistrationSagaOrchestrator sagaOrchestrator,
        ILogger<RegistrationsController> logger)
    {
        _context = context;
        _eventServiceClient = eventServiceClient;
        _eventQueryClient = eventQueryClient;
        _emailQueuePublisher = emailQueuePublisher;
        _sagaOrchestrator = sagaOrchestrator;
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

        return Ok(registrations.Select(r =>
        {
            var e = events.FirstOrDefault(x => x.EventId == r.EventId);
            return new RegistrationDto
            {
                RegistrationId = r.RegistrationId,
                RegistrationDate = r.RegistrationDate,
                EventId = r.EventId,
                EventName = e?.EventName ?? $"Event #{r.EventId}",
                ParticipantId = r.ParticipantId,
                ParticipantFullName = r.Participant != null
                    ? r.Participant.FirstName + " " + r.Participant.LastName : string.Empty,
                ParticipantEmail = r.Participant?.Email ?? string.Empty
            };
        }).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<RegistrationDto>> GetById(long id)
    {
        var registration = await _context.Registrations
            .Include(r => r.Participant)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);

        if (registration == null) return NotFound();

        var eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(registration.EventId);

        return Ok(new RegistrationDto
        {
            RegistrationId = registration.RegistrationId,
            RegistrationDate = registration.RegistrationDate,
            EventId = registration.EventId,
            EventName = eventInfo.EventName,
            ParticipantId = registration.ParticipantId,
            ParticipantFullName = registration.Participant != null
                ? registration.Participant.FirstName + " " + registration.Participant.LastName : string.Empty,
            ParticipantEmail = registration.Participant?.Email ?? string.Empty
        });
    }

    
    [HttpPost]
    public async Task<ActionResult<long>> Create(RegistrationCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var participantExists = await _context.Participants
            .AnyAsync(p => p.ParticipantId == dto.ParticipantId);
        if (!participantExists)
            return BadRequest("Selected participant does not exist.");

        _logger.LogInformation("Attempting Request-Reply for EventId={Id}.", dto.EventId);

        var mqReply = await _eventQueryClient.QueryEventInfoAsync(dto.EventId, HttpContext.RequestAborted);

        EventRegistrationInfoDto eventInfo;
        if (mqReply is not null)
        {
            _logger.LogInformation(
                "Event info via RabbitMQ. EventId={Id}, Exists={E}.", dto.EventId, mqReply.Exists);
            eventInfo = new EventRegistrationInfoDto
            { EventName = mqReply.EventName, Exists = mqReply.Exists, Capacity = mqReply.Capacity };
        }
        else
        {
            _logger.LogWarning("RabbitMQ timeout, falling back to HTTP for EventId={Id}.", dto.EventId);
            eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(dto.EventId);
        }

        if (!eventInfo.Exists)
            return BadRequest("Selected event does not exist.");

        if (await AlreadyRegistered(dto.EventId, dto.ParticipantId))
            return BadRequest("This participant is already registered for the selected event.");

        // Dohvati podatke o učesniku i događaju za Sagu
        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.ParticipantId == dto.ParticipantId);

        if (participant == null)
            return BadRequest("Selected participant does not exist.");

        EventDto? eventDetails = null;
        try
        {
            var allEvents = await _eventServiceClient.GetAllEventsAsync();
            eventDetails = allEvents.FirstOrDefault(e => e.EventId == dto.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nije uspelo dohvatanje detalja događaja za LocationId.");
        }

        long locationId = eventDetails?.LocationId ?? 0;

        // ── POKRETANJE SAGE ─────────────────────────────────────────────────
        _logger.LogInformation(
            "Pokrećem Saga Orkestraciju za EventId={EventId}, ParticipantId={ParticipantId}.",
            dto.EventId, dto.ParticipantId);

        var sagaResult = await _sagaOrchestrator.ExecuteAsync(
            eventId: dto.EventId,
            participantId: dto.ParticipantId,
            registrationDate: dto.RegistrationDate,
            locationId: locationId,
            eventName: eventInfo.EventName,
            participantFirstName: participant.FirstName,
            participantLastName: participant.LastName,
            participantEmail: participant.Email,
            cancellationToken: HttpContext.RequestAborted);

        if (!sagaResult.Success)
        {
            _logger.LogWarning(
                "Saga nije uspela: {Error}", sagaResult.ErrorMessage);
            return BadRequest(sagaResult.ErrorMessage ?? "Registracija nije uspela.");
        }

        return CreatedAtAction(nameof(GetById),
            new { id = sagaResult.RegistrationId }, sagaResult.RegistrationId);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, RegistrationCreateUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var registration = await _context.Registrations.FindAsync(id);
        if (registration == null) return NotFound();

        var participantExists = await _context.Participants.AnyAsync(p => p.ParticipantId == dto.ParticipantId);
        if (!participantExists) return BadRequest("Selected participant does not exist.");

        var eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(dto.EventId);
        if (!eventInfo.Exists) return BadRequest("Selected event does not exist.");

        if (await DuplicateRegistrationExistsAsync(dto.EventId, dto.ParticipantId, id))
            return BadRequest("This participant is already registered for the selected event.");

        if (await IsEventCapacityReached(dto.EventId, eventInfo.Capacity, id))
            return BadRequest("Registration is not possible because the event location capacity has been reached.");

        var oldEventId = registration.EventId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            registration.EventId = dto.EventId;
            registration.ParticipantId = dto.ParticipantId;
            registration.RegistrationDate = dto.RegistrationDate;
            await _context.SaveChangesAsync();

            if (oldEventId != dto.EventId)
            {
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(RegistrationDeletedEvent),
                    Payload = JsonSerializer.Serialize(new RegistrationDeletedEvent
                    { RegistrationId = registration.RegistrationId, EventId = oldEventId }),
                    CreatedAt = DateTime.UtcNow
                });
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(RegistrationCreatedEvent),
                    Payload = JsonSerializer.Serialize(new RegistrationCreatedEvent
                    {
                        RegistrationId = registration.RegistrationId,
                        EventId = dto.EventId,
                        ParticipantId = dto.ParticipantId,
                        RegistrationDate = dto.RegistrationDate
                    }),
                    CreatedAt = DateTime.UtcNow.AddMilliseconds(1)
                });
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            if (!await RegistrationExistsAsync(id)) return NotFound();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greska pri azuriranju registracije.");
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpGet("{id:long}/delete-info")]
    public async Task<ActionResult<RegistrationDto>> GetDeleteInfo(long id)
    {
        var registration = await _context.Registrations
            .Include(r => r.Participant)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);

        if (registration == null) return NotFound();

        var eventInfo = await _eventServiceClient.GetRegistrationInfoAsync(registration.EventId);

        return Ok(new RegistrationDto
        {
            RegistrationId = registration.RegistrationId,
            RegistrationDate = registration.RegistrationDate,
            EventId = registration.EventId,
            EventName = eventInfo.EventName,
            ParticipantId = registration.ParticipantId,
            ParticipantFullName = registration.Participant != null
                ? registration.Participant.FirstName + " " + registration.Participant.LastName : string.Empty,
            ParticipantEmail = registration.Participant?.Email ?? string.Empty
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var registration = await _context.Registrations.FindAsync(id);
        if (registration == null) return NotFound();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.OutboxMessages.Add(new OutboxMessage
            {
                EventType = nameof(RegistrationDeletedEvent),
                Payload = JsonSerializer.Serialize(new RegistrationDeletedEvent
                { RegistrationId = registration.RegistrationId, EventId = registration.EventId }),
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
        => Ok(await _context.Registrations.AnyAsync(r => r.EventId == eventId));

    private Task<bool> RegistrationExistsAsync(long id)
        => _context.Registrations.AnyAsync(r => r.RegistrationId == id);

    private Task<bool> AlreadyRegistered(long eventId, long participantId)
        => _context.Registrations.AnyAsync(r => r.EventId == eventId && r.ParticipantId == participantId);

    private Task<bool> DuplicateRegistrationExistsAsync(long eventId, long participantId, long excludeId)
        => _context.Registrations.AnyAsync(r =>
            r.RegistrationId != excludeId && r.EventId == eventId && r.ParticipantId == participantId);

    private async Task<bool> IsEventCapacityReached(long eventId, int capacity, long? excludeId = null)
    {
        var q = _context.Registrations.Where(r => r.EventId == eventId);
        if (excludeId.HasValue) q = q.Where(r => r.RegistrationId != excludeId.Value);
        return await q.CountAsync() >= capacity;
    }
}