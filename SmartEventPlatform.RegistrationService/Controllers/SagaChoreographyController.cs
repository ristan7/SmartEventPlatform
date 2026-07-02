using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.RegistrationService.Clients;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Messaging;
using SmartEventPlatform.RegistrationService.Models;
using System.Text.Json;

namespace SmartEventPlatform.RegistrationService.Controllers;

[ApiController]
[Route("api/saga-choreography")]
public class SagaChoreographyController : ControllerBase
{
    private readonly RegistrationDbContext _context;
    private readonly IEventServiceClient _eventServiceClient;
    private readonly IRabbitMqEventQueryClient _eventQueryClient;
    private readonly ISagaChoreographyPublisher _sagaPublisher;
    private readonly IOptions<SagaChoreographyRabbitMqOptions> _mqOptions;
    private readonly ILogger<SagaChoreographyController> _logger;

    public SagaChoreographyController(
        RegistrationDbContext context,
        IEventServiceClient eventServiceClient,
        IRabbitMqEventQueryClient eventQueryClient,
        ISagaChoreographyPublisher sagaPublisher,
        IOptions<SagaChoreographyRabbitMqOptions> mqOptions,
        ILogger<SagaChoreographyController> logger)
    {
        _context = context;
        _eventServiceClient = eventServiceClient;
        _eventQueryClient = eventQueryClient;
        _sagaPublisher = sagaPublisher;
        _mqOptions = mqOptions;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] SagaChoreographyStartRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.ParticipantId == request.ParticipantId);
        if (participant is null)
            return BadRequest("Selected participant does not exist.");

        // Provjera dogadjaja (Request-Reply → HTTP fallback)
        var mqReply = await _eventQueryClient.QueryEventInfoAsync(request.EventId, HttpContext.RequestAborted);

        string eventName;
        bool eventExists;
        if (mqReply is not null)
        {
            eventName = mqReply.EventName;
            eventExists = mqReply.Exists;
        }
        else
        {
            _logger.LogWarning("[SagaChoreo] RabbitMQ timeout, HTTP fallback za EventId={Id}.", request.EventId);
            var info = await _eventServiceClient.GetRegistrationInfoAsync(request.EventId);
            eventName = info.EventName;
            eventExists = info.Exists;
        }

        if (!eventExists)
            return BadRequest("Selected event does not exist.");

        if (await _context.Registrations.AnyAsync(r =>
                r.EventId == request.EventId && r.ParticipantId == request.ParticipantId))
            return BadRequest("This participant is already registered for the selected event.");

        // Dohvati LocationId
        long locationId = 0;
        try
        {
            var allEvents = await _eventServiceClient.GetAllEventsAsync();
            locationId = allEvents.FirstOrDefault(e => e.EventId == request.EventId)?.LocationId ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SagaChoreo] Nije uspjelo dohvatanje LocationId za EventId={Id}.", request.EventId);
        }

        
        var correlationId = Guid.NewGuid();

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var registration = new Registration
            {
                EventId = request.EventId,
                ParticipantId = request.ParticipantId,
                RegistrationDate = request.RegistrationDate,
                Status = "Pending"
            };
            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            var sagaState = new SagaChoreographyState
            {
                CorrelationId = correlationId,
                Status = "Started",
                RegistrationId = registration.RegistrationId,
                EventId = request.EventId,
                ParticipantId = request.ParticipantId,
                LocationId = locationId,
                ParticipantFirstName = participant.FirstName,
                ParticipantLastName = participant.LastName,
                ParticipantEmail = participant.Email,
                EventName = eventName,
                RegistrationDate = request.RegistrationDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.SagaChoreographyStates.Add(sagaState);
            await _context.SaveChangesAsync();

            await tx.CommitAsync();

            _logger.LogInformation(
                "[SagaChoreo] Saga pokrenuta. CorrelationId={CorrId}, RegistrationId={RegId}.",
                correlationId, registration.RegistrationId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "[SagaChoreo] Greska pri kreiranju Saga state/registracije.");
            return StatusCode(500, "Error starting the saga.");
        }

        // Objavi SagaChoreographyStarted → EventService
        var startedEvent = new SagaChoreographyStartedEvent
        {
            CorrelationId = correlationId,
            EventId = request.EventId,
            ParticipantId = request.ParticipantId,
            LocationId = locationId,
            RegistrationDate = request.RegistrationDate,
            ParticipantFirstName = participant.FirstName,
            ParticipantLastName = participant.LastName,
            ParticipantEmail = participant.Email,
            EventName = eventName,
            OccurredAt = DateTime.UtcNow
        };

        await _sagaPublisher.PublishAsync(
            routingKey: _mqOptions.Value.EventServiceRoutingKey,
            payload: JsonSerializer.Serialize(startedEvent),
            messageType: nameof(SagaChoreographyStartedEvent),
            cancellationToken: HttpContext.RequestAborted);

        _logger.LogInformation("[SagaChoreo] SagaChoreographyStarted objavljen. CorrelationId={CorrId}.", correlationId);

        return Accepted(new
        {
            CorrelationId = correlationId,
            Message = "Saga Choreography has been started. Track the status using the CorrelationId.",
            StatusUrl = Url.Action(nameof(GetStatus), new { correlationId })
        });
    }

    [HttpGet("{correlationId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid correlationId)
    {
        var saga = await _context.SagaChoreographyStates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);

        if (saga is null)
            return NotFound($"Saga with CorrelationId={correlationId} was not found.");

        return Ok(new
        {
            saga.SagaId,
            saga.CorrelationId,
            saga.Status,
            saga.RegistrationId,
            saga.FailureReason,
            saga.CreatedAt,
            saga.UpdatedAt
        });
    }
}