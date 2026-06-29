using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.EventService.Clients;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Messaging;
using SmartEventPlatform.EventService.Models;
using System.Text.Json;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly EventDbContext _context;
        private readonly IDirectoryServiceClient _directoryServiceClient;
        private readonly ILogger<EventsController> _logger;
        private readonly PublisherRabbitMqOptions _publisherOptions;

        public EventsController(
            EventDbContext context,
            IDirectoryServiceClient directoryServiceClient,
            ILogger<EventsController> logger,
            Microsoft.Extensions.Options.IOptions<PublisherRabbitMqOptions> publisherOptions)
        {
            _context = context;
            _directoryServiceClient = directoryServiceClient;
            _logger = logger;
            _publisherOptions = publisherOptions.Value;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetAll()
        {
            var events = await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.EventSpeakers)
                .OrderBy(e => e.EventDateTime)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Agenda = e.Agenda,
                    EventDateTime = e.EventDateTime,
                    DurationInMinutes = e.DurationInMinutes,
                    RegistrationFee = e.RegistrationFee,

                    LocationId = e.LocationId,
                    LocationName = e.LocationNameSnapshot,
                    LocationAddress = e.LocationAddressSnapshot,
                    Capacity = e.LocationCapacitySnapshot,

                    EventTypeId = e.EventTypeId,
                    EventTypeName = e.EventType != null ? e.EventType.Name : string.Empty,

                    Speakers = e.EventSpeakers
                        .OrderBy(es => es.Time)
                        .Select(es => es.SpeakerFullNameSnapshot)
                        .ToList()
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<EventDto>> GetById(long id)
        {
            var eventDto = await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.EventSpeakers)
                .Where(e => e.EventId == id)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Agenda = e.Agenda,
                    EventDateTime = e.EventDateTime,
                    DurationInMinutes = e.DurationInMinutes,
                    RegistrationFee = e.RegistrationFee,

                    LocationId = e.LocationId,
                    LocationName = e.LocationNameSnapshot,
                    LocationAddress = e.LocationAddressSnapshot,
                    Capacity = e.LocationCapacitySnapshot,

                    EventTypeId = e.EventTypeId,
                    EventTypeName = e.EventType != null ? e.EventType.Name : string.Empty,

                    Speakers = e.EventSpeakers
                        .OrderBy(es => es.Time)
                        .Select(es => es.SpeakerFullNameSnapshot)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (eventDto == null)
                return NotFound();

            return Ok(eventDto);
        }

        [HttpGet("exists-for-location/{locationId:long}")]
        public async Task<ActionResult<bool>> ExistsForLocation(long locationId)
        {
            var exists = await _context.Events
                .AnyAsync(e => e.LocationId == locationId);

            return Ok(exists);
        }

        [HttpGet("exists-for-speaker/{speakerId:long}")]
        public async Task<ActionResult<bool>> ExistsForSpeaker(long speakerId)
        {
            var exists = await _context.EventSpeakers
                .AnyAsync(es => es.SpeakerId == speakerId);

            return Ok(exists);
        }

        [HttpGet("{id:long}/registration-info")]
        public async Task<ActionResult<EventRegistrationInfoDto>> GetRegistrationInfo(long id)
        {
            var dto = await _context.Events
                .Where(e => e.EventId == id)
                .Select(e => new EventRegistrationInfoDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDateTime = e.EventDateTime,
                    Capacity = e.LocationCapacitySnapshot,
                    Exists = true
                })
                .FirstOrDefaultAsync();

            if (dto == null)
            {
                return Ok(new EventRegistrationInfoDto
                {
                    EventId = id,
                    Exists = false
                });
            }

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<long>> Create(EventCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var location = await _directoryServiceClient.GetLocationAsync(dto.LocationId);

            if (location == null)
                return BadRequest("Selected location does not exist.");

            var eventTypeExists = await _context.EventTypes
                .AnyAsync(et => et.EventTypeId == dto.EventTypeId);

            if (!eventTypeExists)
                return BadRequest("Selected event type does not exist.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newEvent = new Event
                {
                    EventName = dto.EventName,
                    Agenda = dto.Agenda,
                    EventDateTime = dto.EventDateTime,
                    DurationInMinutes = dto.DurationInMinutes,
                    RegistrationFee = dto.RegistrationFee,
                    LocationId = dto.LocationId,
                    LocationNameSnapshot = location.LocationName,
                    LocationAddressSnapshot = location.Address,
                    LocationCapacitySnapshot = location.Capacity,
                    EventTypeId = dto.EventTypeId
                };

                _context.Events.Add(newEvent);
                await _context.SaveChangesAsync();

                // Outbox — notify DirectoryService that a new event is using a location.
                // RoutingKey routes this to the location-usage queue.
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(EventCreatedEvent),
                    RoutingKey = _publisherOptions.LocationUsageRoutingKey,
                    Payload = JsonSerializer.Serialize(new EventCreatedEvent
                    {
                        EventId = newEvent.EventId,
                        LocationId = newEvent.LocationId
                    }),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Event created. EventId={EventId}, LocationId={LocationId}.",
                    newEvent.EventId, newEvent.LocationId);

                return CreatedAtAction(nameof(GetById), new { id = newEvent.EventId }, newEvent.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event.");
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, EventCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
                return NotFound();

            var location = await _directoryServiceClient.GetLocationAsync(dto.LocationId);

            if (location == null)
                return BadRequest("Selected location does not exist.");

            var eventTypeExists = await _context.EventTypes
                .AnyAsync(et => et.EventTypeId == dto.EventTypeId);

            if (!eventTypeExists)
                return BadRequest("Selected event type does not exist.");

            var oldLocationId = existingEvent.LocationId;

            existingEvent.EventName = dto.EventName;
            existingEvent.Agenda = dto.Agenda;
            existingEvent.EventDateTime = dto.EventDateTime;
            existingEvent.DurationInMinutes = dto.DurationInMinutes;
            existingEvent.RegistrationFee = dto.RegistrationFee;
            existingEvent.LocationId = dto.LocationId;
            existingEvent.LocationNameSnapshot = location.LocationName;
            existingEvent.LocationAddressSnapshot = location.Address;
            existingEvent.LocationCapacitySnapshot = location.Capacity;
            existingEvent.EventTypeId = dto.EventTypeId;

            // If the location changed, notify DirectoryService:
            // send a "deleted" for the old location and a "created" for the new one.
            // Both messages go to the location-usage routing key.
            if (oldLocationId != dto.LocationId)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();

                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        EventType = nameof(EventDeletedEvent),
                        RoutingKey = _publisherOptions.LocationUsageRoutingKey,
                        Payload = JsonSerializer.Serialize(new EventDeletedEvent
                        {
                            EventId = id,
                            LocationId = oldLocationId
                        }),
                        CreatedAt = DateTime.UtcNow
                    });

                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        EventType = nameof(EventCreatedEvent),
                        RoutingKey = _publisherOptions.LocationUsageRoutingKey,
                        Payload = JsonSerializer.Serialize(new EventCreatedEvent
                        {
                            EventId = id,
                            LocationId = dto.LocationId
                        }),
                        CreatedAt = DateTime.UtcNow.AddMilliseconds(1)
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Event updated with location change. EventId={EventId}, OldLocationId={OldLocationId}, NewLocationId={NewLocationId}.",
                        id, oldLocationId, dto.LocationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating event. EventId={EventId}.", id);
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                try { await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await EventExistsAsync(id)) return NotFound();
                    throw;
                }
            }

            return NoContent();
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<EventDto>> GetDeleteInfo(long id)
        {
            var eventDto = await _context.Events
                .Include(e => e.EventType)
                .Where(e => e.EventId == id)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDateTime = e.EventDateTime,
                    LocationId = e.LocationId,
                    LocationName = e.LocationNameSnapshot,
                    LocationAddress = e.LocationAddressSnapshot,
                    Capacity = e.LocationCapacitySnapshot,
                    EventTypeId = e.EventTypeId,
                    EventTypeName = e.EventType != null ? e.EventType.Name : string.Empty,
                    Agenda = e.Agenda,
                    DurationInMinutes = e.DurationInMinutes,
                    RegistrationFee = e.RegistrationFee
                })
                .FirstOrDefaultAsync();

            if (eventDto == null)
                return NotFound();

            return Ok(eventDto);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existingEvent = await _context.Events
                .Include(e => e.EventSpeakers)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
                return NotFound();

            var deleteErrors = new List<string>();

            if (existingEvent.EventSpeakers.Any())
                deleteErrors.Add("This event cannot be deleted because it has assigned speakers.");

            var hasRegistrations = await _context.EventRegistrationTrackers
                .AnyAsync(t => t.EventId == id && t.RegistrationCount > 0);

            if (hasRegistrations)
                deleteErrors.Add("This event cannot be deleted because it has participant registrations.");

            if (deleteErrors.Any())
                return BadRequest(string.Join(" ", deleteErrors));

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var locationId = existingEvent.LocationId;

                _context.Events.Remove(existingEvent);
                await _context.SaveChangesAsync();

                // Outbox — notify DirectoryService that the event no longer uses this location.
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(EventDeletedEvent),
                    RoutingKey = _publisherOptions.LocationUsageRoutingKey,
                    Payload = JsonSerializer.Serialize(new EventDeletedEvent
                    {
                        EventId = id,
                        LocationId = locationId
                    }),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Event deleted. EventId={EventId}, LocationId={LocationId}.",
                    id, locationId);

                return NoContent();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return BadRequest("This event cannot be deleted because it is used by other records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event. EventId={EventId}.", id);
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<bool> EventExistsAsync(long id)
        {
            return await _context.Events.AnyAsync(e => e.EventId == id);
        }
    }
}