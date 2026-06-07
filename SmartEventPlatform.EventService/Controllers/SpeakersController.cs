using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatformWeb.EventService.Models;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpeakersController : ControllerBase
    {
        private readonly EventDbContext _context;

        public SpeakersController(EventDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SpeakerDto>>> GetAll()
        {
            var speakers = await _context.Speakers
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new SpeakerDto
                {
                    SpeakerId = s.SpeakerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Title = s.Title,
                    ExpertiseAreas = s.ExpertiseAreas
                })
                .ToListAsync();

            return Ok(speakers);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<SpeakerDto>> GetById(long id)
        {
            var speaker = await _context.Speakers
                .Include(s => s.EventSpeakers)
                    .ThenInclude(es => es.Event)
                .Where(s => s.SpeakerId == id)
                .Select(s => new SpeakerDto
                {
                    SpeakerId = s.SpeakerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Title = s.Title,
                    ExpertiseAreas = s.ExpertiseAreas,

                    EventSpeakersParticipations = s.EventSpeakers
                        .OrderBy(es => es.Time)
                        .Select(es => new SpeakerEventItemDto
                        {
                            EventSpeakerId = es.EventSpeakerId,
                            EventId = es.EventId,
                            EventName = es.Event != null ? es.Event.EventName : string.Empty,
                            Topic = es.Topic,
                            Time = es.Time
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (speaker == null)
            {
                return NotFound();
            }

            return Ok(speaker);
        }

        [HttpPost]
        public async Task<ActionResult<long>> Create(SpeakerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var speaker = new Speaker
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Title = dto.Title,
                ExpertiseAreas = dto.ExpertiseAreas
            };

            _context.Speakers.Add(speaker);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = speaker.SpeakerId }, speaker.SpeakerId);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, SpeakerDto dto)
        {
            if (id != dto.SpeakerId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var speaker = await _context.Speakers.FindAsync(id);

            if (speaker == null)
            {
                return NotFound();
            }

            speaker.FirstName = dto.FirstName;
            speaker.LastName = dto.LastName;
            speaker.Title = dto.Title;
            speaker.ExpertiseAreas = dto.ExpertiseAreas;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SpeakerExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<SpeakerDto>> GetDeleteInfo(long id)
        {
            var speaker = await _context.Speakers
                .Where(s => s.SpeakerId == id)
                .Select(s => new SpeakerDto
                {
                    SpeakerId = s.SpeakerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Title = s.Title,
                    ExpertiseAreas = s.ExpertiseAreas
                })
                .FirstOrDefaultAsync();

            if (speaker == null)
            {
                return NotFound();
            }

            return Ok(speaker);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var speaker = await _context.Speakers.FindAsync(id);

            if (speaker == null)
            {
                return NotFound();
            }

            var hasEventSpeakers = await _context.EventSpeakers
                .AnyAsync(es => es.SpeakerId == id);

            if (hasEventSpeakers)
            {
                return BadRequest("This speaker cannot be deleted because they are assigned to one or more events.");
            }

            try
            {
                _context.Speakers.Remove(speaker);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return BadRequest("This speaker cannot be deleted because they are assigned to one or more events.");
            }
        }

        private async Task<bool> SpeakerExistsAsync(long id)
        {
            return await _context.Speakers.AnyAsync(s => s.SpeakerId == id);
        }
    }
}
