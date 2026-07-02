using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Speakers;
using SmartEventPlatform.DirectoryService.Data;
using SmartEventPlatform.DirectoryService.Models;

namespace SmartEventPlatform.DirectoryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpeakersController : ControllerBase
    {
        private readonly DirectoryDbContext _context;

        public SpeakersController(DirectoryDbContext context)
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

            // DirectoryService ne zove EventService direktno.
            // Upotreba predavača se prati lokalno kroz SpeakerUsageTrackers,
            // a tabela se ažurira asinhrono porukama iz EventService-a.
            var hasEventSpeakers = await _context.SpeakerUsageTrackers
                .AnyAsync(t => t.SpeakerId == id);

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