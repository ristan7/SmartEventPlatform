using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.EventSpeakers;
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
    public class EventSpeakersController : ControllerBase
    {
        private readonly EventDbContext _context;
        private readonly IDirectoryServiceClient _directoryServiceClient;
        private readonly ILogger<EventSpeakersController> _logger;
        private readonly PublisherRabbitMqOptions _publisherOptions;

        public EventSpeakersController(
            EventDbContext context,
            IDirectoryServiceClient directoryServiceClient,
            ILogger<EventSpeakersController> logger,
            Microsoft.Extensions.Options.IOptions<PublisherRabbitMqOptions> publisherOptions)
        {
            _context = context;
            _directoryServiceClient = directoryServiceClient;
            _logger = logger;
            _publisherOptions = publisherOptions.Value;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventSpeakerDto>>> GetAll()
        {
            var eventSpeakers = await _context.EventSpeakers
                .Include(es => es.Event)
                .OrderBy(es => es.Event != null ? es.Event.EventName : string.Empty)
                .ThenBy(es => es.Time)
                .Select(es => new EventSpeakerDto
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventId = es.EventId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerId = es.SpeakerId,
                    SpeakerFullName = es.SpeakerFullNameSnapshot,
                    Topic = es.Topic,
                    Time = es.Time
                })
                .ToListAsync();

            return Ok(eventSpeakers);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<EventSpeakerDto>> GetById(long id)
        {
            var eventSpeaker = await _context.EventSpeakers
                .Include(es => es.Event)
                .Where(es => es.EventSpeakerId == id)
                .Select(es => new EventSpeakerDto
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventId = es.EventId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerId = es.SpeakerId,
                    SpeakerFullName = es.SpeakerFullNameSnapshot,
                    Topic = es.Topic,
                    Time = es.Time
                })
                .FirstOrDefaultAsync();

            if (eventSpeaker == null)
                return NotFound();

            return Ok(eventSpeaker);
        }

        [HttpGet("by-speaker/{speakerId:long}")]
        public async Task<ActionResult<IEnumerable<EventSpeakerDto>>> GetBySpeaker(long speakerId)
        {
            var eventSpeakers = await _context.EventSpeakers
                .Include(es => es.Event)
                .Where(es => es.SpeakerId == speakerId)
                .OrderBy(es => es.Time)
                .Select(es => new EventSpeakerDto
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventId = es.EventId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerId = es.SpeakerId,
                    SpeakerFullName = es.SpeakerFullNameSnapshot,
                    Topic = es.Topic,
                    Time = es.Time
                })
                .ToListAsync();

            return Ok(eventSpeakers);
        }

        [HttpPost]
        public async Task<ActionResult<long>> Create(EventSpeakerCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var eventExists = await _context.Events.AnyAsync(e => e.EventId == dto.EventId);

            if (!eventExists)
                return BadRequest("Selected event does not exist.");

            var speaker = await _directoryServiceClient.GetSpeakerAsync(dto.SpeakerId);

            if (speaker == null)
                return BadRequest("Selected speaker does not exist.");

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(dto.EventId, dto.Time);

            if (!isTimeValid)
                return BadRequest("Speaker time must be within the selected event duration.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var eventSpeaker = new EventSpeaker
                {
                    EventId = dto.EventId,
                    SpeakerId = dto.SpeakerId,
                    SpeakerFullNameSnapshot = speaker.FullName,
                    Topic = dto.Topic,
                    Time = dto.Time
                };

                _context.EventSpeakers.Add(eventSpeaker);
                await _context.SaveChangesAsync();

                // Outbox — notify DirectoryService that a speaker now has an engagement.
                // RoutingKey routes this to the speaker-usage queue.
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(EventSpeakerAddedEvent),
                    RoutingKey = _publisherOptions.SpeakerUsageRoutingKey,
                    Payload = JsonSerializer.Serialize(new EventSpeakerAddedEvent
                    {
                        EventSpeakerId = eventSpeaker.EventSpeakerId,
                        SpeakerId = eventSpeaker.SpeakerId
                    }),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "EventSpeaker created. EventSpeakerId={EventSpeakerId}, SpeakerId={SpeakerId}.",
                    eventSpeaker.EventSpeakerId, eventSpeaker.SpeakerId);

                return CreatedAtAction(nameof(GetById), new { id = eventSpeaker.EventSpeakerId }, eventSpeaker.EventSpeakerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event speaker.");
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, EventSpeakerCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var eventSpeaker = await _context.EventSpeakers.FindAsync(id);

            if (eventSpeaker == null)
                return NotFound();

            var eventExists = await _context.Events.AnyAsync(e => e.EventId == dto.EventId);

            if (!eventExists)
                return BadRequest("Selected event does not exist.");

            var speaker = await _directoryServiceClient.GetSpeakerAsync(dto.SpeakerId);

            if (speaker == null)
                return BadRequest("Selected speaker does not exist.");

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(dto.EventId, dto.Time);

            if (!isTimeValid)
                return BadRequest("Speaker time must be within the selected event duration.");

            var oldSpeakerId = eventSpeaker.SpeakerId;

            eventSpeaker.EventId = dto.EventId;
            eventSpeaker.SpeakerId = dto.SpeakerId;
            eventSpeaker.SpeakerFullNameSnapshot = speaker.FullName;
            eventSpeaker.Topic = dto.Topic;
            eventSpeaker.Time = dto.Time;

            // If the speaker changed, notify DirectoryService:
            // send "removed" for the old speaker and "added" for the new one.
            // Both messages go to the speaker-usage routing key.
            if (oldSpeakerId != dto.SpeakerId)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();

                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        EventType = nameof(EventSpeakerRemovedEvent),
                        RoutingKey = _publisherOptions.SpeakerUsageRoutingKey,
                        Payload = JsonSerializer.Serialize(new EventSpeakerRemovedEvent
                        {
                            EventSpeakerId = id,
                            SpeakerId = oldSpeakerId
                        }),
                        CreatedAt = DateTime.UtcNow
                    });

                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        EventType = nameof(EventSpeakerAddedEvent),
                        RoutingKey = _publisherOptions.SpeakerUsageRoutingKey,
                        Payload = JsonSerializer.Serialize(new EventSpeakerAddedEvent
                        {
                            EventSpeakerId = id,
                            SpeakerId = dto.SpeakerId
                        }),
                        CreatedAt = DateTime.UtcNow.AddMilliseconds(1)
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "EventSpeaker updated with speaker change. EventSpeakerId={EventSpeakerId}, OldSpeakerId={OldSpeakerId}, NewSpeakerId={NewSpeakerId}.",
                        id, oldSpeakerId, dto.SpeakerId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating event speaker. EventSpeakerId={EventSpeakerId}.", id);
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                try { await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await EventSpeakerExistsAsync(id)) return NotFound();
                    throw;
                }
            }

            return NoContent();
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<EventSpeakerDto>> GetDeleteInfo(long id)
        {
            var eventSpeaker = await _context.EventSpeakers
                .Include(es => es.Event)
                .Where(es => es.EventSpeakerId == id)
                .Select(es => new EventSpeakerDto
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventId = es.EventId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerId = es.SpeakerId,
                    SpeakerFullName = es.SpeakerFullNameSnapshot,
                    Topic = es.Topic,
                    Time = es.Time
                })
                .FirstOrDefaultAsync();

            if (eventSpeaker == null)
                return NotFound();

            return Ok(eventSpeaker);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var eventSpeaker = await _context.EventSpeakers.FindAsync(id);

            if (eventSpeaker == null)
                return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var speakerId = eventSpeaker.SpeakerId;

                _context.EventSpeakers.Remove(eventSpeaker);
                await _context.SaveChangesAsync();

                // Outbox — notify DirectoryService that the speaker no longer has this engagement.
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(EventSpeakerRemovedEvent),
                    RoutingKey = _publisherOptions.SpeakerUsageRoutingKey,
                    Payload = JsonSerializer.Serialize(new EventSpeakerRemovedEvent
                    {
                        EventSpeakerId = id,
                        SpeakerId = speakerId
                    }),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "EventSpeaker deleted. EventSpeakerId={EventSpeakerId}, SpeakerId={SpeakerId}.",
                    id, speakerId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event speaker. EventSpeakerId={EventSpeakerId}.", id);
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet("exists-for-speaker/{speakerId:long}")]
        public async Task<ActionResult<bool>> ExistsForSpeaker(long speakerId)
        {
            var exists = await _context.EventSpeakers.AnyAsync(es => es.SpeakerId == speakerId);
            return Ok(exists);
        }

        private async Task<bool> EventSpeakerExistsAsync(long id)
        {
            return await _context.EventSpeakers.AnyAsync(es => es.EventSpeakerId == id);
        }

        private async Task<bool> IsSpeakerTimeInsideEventAsync(long eventId, DateTime speakerTime)
        {
            var selectedEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);

            if (selectedEvent == null)
                return false;

            var eventStart = selectedEvent.EventDateTime;
            var eventEnd = selectedEvent.EventDateTime.AddMinutes(selectedEvent.DurationInMinutes);

            return speakerTime >= eventStart && speakerTime <= eventEnd;
        }
    }
}