using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Models;

namespace SmartEventPlatform.RegistrationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParticipantsController : ControllerBase
    {
        private readonly RegistrationDbContext _context;

        public ParticipantsController(RegistrationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ParticipantDto>>> GetAll()
        {
            var participants = await _context.Participants
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new ParticipantDto
                {
                    ParticipantId = p.ParticipantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email
                })
                .ToListAsync();

            return Ok(participants);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ParticipantDto>> GetById(long id)
        {
            var participant = await _context.Participants
                .Where(p => p.ParticipantId == id)
                .Select(p => new ParticipantDto
                {
                    ParticipantId = p.ParticipantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email
                })
                .FirstOrDefaultAsync();

            if (participant == null)
            {
                return NotFound();
            }

            return Ok(participant);
        }

        [HttpPost]
        public async Task<ActionResult<long>> Create(ParticipantDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var emailAlreadyExists = await _context.Participants
                .AnyAsync(p => p.Email == dto.Email);

            if (emailAlreadyExists)
            {
                return BadRequest("Participant with this email already exists.");
            }

            var participant = new Participant
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = participant.ParticipantId }, participant.ParticipantId);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, ParticipantDto dto)
        {
            if (id != dto.ParticipantId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var participant = await _context.Participants.FindAsync(id);

            if (participant == null)
            {
                return NotFound();
            }

            var emailAlreadyExists = await _context.Participants
                .AnyAsync(p => p.Email == dto.Email && p.ParticipantId != id);

            if (emailAlreadyExists)
            {
                return BadRequest("Participant with this email already exists.");
            }

            participant.FirstName = dto.FirstName;
            participant.LastName = dto.LastName;
            participant.Email = dto.Email;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ParticipantExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<ParticipantDto>> GetDeleteInfo(long id)
        {
            var participant = await _context.Participants
                .Where(p => p.ParticipantId == id)
                .Select(p => new ParticipantDto
                {
                    ParticipantId = p.ParticipantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email
                })
                .FirstOrDefaultAsync();

            if (participant == null)
            {
                return NotFound();
            }

            return Ok(participant);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var participant = await _context.Participants.FindAsync(id);

            if (participant == null)
            {
                return NotFound();
            }

            var hasRegistrations = participant.Registrations.Any();

            if (hasRegistrations)
            {
                return BadRequest("This participant cannot be deleted because they have one or more event registrations.");
            }

            try
            {
                _context.Participants.Remove(participant);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return BadRequest("This participant cannot be deleted because they have one or more event registrations.");
            }
        }

        private async Task<bool> ParticipantExistsAsync(long id)
        {
            return await _context.Participants.AnyAsync(p => p.ParticipantId == id);
        }
    }
}
