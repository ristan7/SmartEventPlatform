using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.EventService.Clients;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventSpeakersController : ControllerBase
    {
        private readonly EventDbContext _context;
        private readonly IDirectoryServiceClient _directoryServiceClient;

        public EventSpeakersController(EventDbContext context, IDirectoryServiceClient directoryServiceClient)
        {
            _context = context;
            _directoryServiceClient = directoryServiceClient;
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
            {
                return NotFound();
            }

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
            {
                return ValidationProblem(ModelState);
            }

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == dto.EventId);

            if (!eventExists)
            {
                return BadRequest("Selected event does not exist.");
            }

            var speaker = await _directoryServiceClient.GetSpeakerAsync(dto.SpeakerId);

            if (speaker == null)
            {
                return BadRequest("Selected speaker does not exist.");
            }

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(dto.EventId, dto.Time);

            if (!isTimeValid)
            {
                return BadRequest("Speaker time must be within the selected event duration.");
            }

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

            return CreatedAtAction(nameof(GetById), new { id = eventSpeaker.EventSpeakerId }, eventSpeaker.EventSpeakerId);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, EventSpeakerCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var eventSpeaker = await _context.EventSpeakers.FindAsync(id);

            if (eventSpeaker == null)
            {
                return NotFound();
            }

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventId == dto.EventId);

            if (!eventExists)
            {
                return BadRequest("Selected event does not exist.");
            }

            var speaker = await _directoryServiceClient.GetSpeakerAsync(dto.SpeakerId);

            if (speaker == null)
            {
                return BadRequest("Selected speaker does not exist.");
            }

            var isTimeValid = await IsSpeakerTimeInsideEventAsync(dto.EventId, dto.Time);

            if (!isTimeValid)
            {
                return BadRequest("Speaker time must be within the selected event duration.");
            }

            eventSpeaker.EventId = dto.EventId;
            eventSpeaker.SpeakerId = dto.SpeakerId;
            eventSpeaker.SpeakerFullNameSnapshot = speaker.FullName;
            eventSpeaker.Topic = dto.Topic;
            eventSpeaker.Time = dto.Time;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await EventSpeakerExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
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
            {
                return NotFound();
            }

            return Ok(eventSpeaker);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var eventSpeaker = await _context.EventSpeakers.FindAsync(id);

            if (eventSpeaker == null)
            {
                return NotFound();
            }

            _context.EventSpeakers.Remove(eventSpeaker);
            await _context.SaveChangesAsync();

            return NoContent();
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
            var selectedEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (selectedEvent == null)
            {
                return false;
            }

            var eventStart = selectedEvent.EventDateTime;
            var eventEnd = selectedEvent.EventDateTime.AddMinutes(selectedEvent.DurationInMinutes);

            return speakerTime >= eventStart && speakerTime <= eventEnd;
        }
    }
}
