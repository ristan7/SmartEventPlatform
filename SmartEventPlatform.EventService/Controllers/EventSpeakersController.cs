using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventSpeakersController : ControllerBase
    {
        private readonly EventDbContext _context;

        public EventSpeakersController(EventDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventSpeakerDto>>> GetAll()
        {
            var eventSpeakers = await _context.EventSpeakers
                .Include(es => es.Event)
                .Include(es => es.Speaker)
                .OrderBy(es => es.Event != null ? es.Event.EventName : string.Empty)
                .ThenBy(es => es.Time)
                .Select(es => new EventSpeakerDto
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventId = es.EventId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerId = es.SpeakerId,
                    SpeakerFullName = es.Speaker != null
                        ? es.Speaker.FirstName + " " + es.Speaker.LastName
                        : string.Empty,
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
                .Include(es => es.Speaker)
                .Where(es => es.EventSpeakerId == id)
                .Select(es => new EventSpeakerDto
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventId = es.EventId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerId = es.SpeakerId,
                    SpeakerFullName = es.Speaker != null
                        ? es.Speaker.FirstName + " " + es.Speaker.LastName
                        : string.Empty,
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

            var speakerExists = await _context.Speakers
                .AnyAsync(s => s.SpeakerId == dto.SpeakerId);

            if (!speakerExists)
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

            var speakerExists = await _context.Speakers
                .AnyAsync(s => s.SpeakerId == dto.SpeakerId);

            if (!speakerExists)
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
                .Include(es => es.Speaker)
                .Where(es => es.EventSpeakerId == id)
                .Select(es => new EventSpeakerDto
                {
                    EventSpeakerId = es.EventSpeakerId,
                    EventId = es.EventId,
                    EventName = es.Event != null ? es.Event.EventName : string.Empty,
                    SpeakerId = es.SpeakerId,
                    SpeakerFullName = es.Speaker != null
                        ? es.Speaker.FirstName + " " + es.Speaker.LastName
                        : string.Empty,
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
