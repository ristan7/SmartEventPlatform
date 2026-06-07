using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.EventTypes;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatformWeb.EventService.Models;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventTypesController : ControllerBase
    {
        private readonly EventDbContext _context;

        public EventTypesController(EventDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventTypeDto>>> GetAll()
        {
            var eventTypes = await _context.EventTypes
                .OrderBy(t => t.Name)
                .Select(t => new EventTypeDto
                {
                    EventTypeId = t.EventTypeId,
                    Name = t.Name
                })
                .ToListAsync();

            return Ok(eventTypes);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<EventTypeDto>> GetById(long id)
        {
            var eventType = await _context.EventTypes
                .Where(t => t.EventTypeId == id)
                .Select(t => new EventTypeDto
                {
                    EventTypeId = t.EventTypeId,
                    Name = t.Name
                })
                .FirstOrDefaultAsync();

            if (eventType == null)
            {
                return NotFound();
            }

            return Ok(eventType);
        }

        
        [HttpPost]
        public async Task<ActionResult<long>> Create(EventTypeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var eventType = new EventType
            {
                Name = dto.Name
            };

            _context.EventTypes.Add(eventType);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = eventType.EventTypeId }, eventType.EventTypeId);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, EventTypeDto dto)
        {
            if (id != dto.EventTypeId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var eventType = await _context.EventTypes.FindAsync(id);

            if (eventType == null)
            {
                return NotFound();
            }

            eventType.Name = dto.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await EventTypeExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<EventTypeDto>> GetDeleteInfo(long id)
        {
            var eventType = await _context.EventTypes
                .Where(t => t.EventTypeId == id)
                .Select(t => new EventTypeDto
                {
                    EventTypeId = t.EventTypeId,
                    Name = t.Name
                })
                .FirstOrDefaultAsync();

            if (eventType == null)
            {
                return NotFound();
            }

            return Ok(eventType);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var eventType = await _context.EventTypes.FindAsync(id);

            if (eventType == null)
            {
                return NotFound();
            }

            var hasEvents = await _context.Events
                .AnyAsync(e => e.EventTypeId == id);

            if (hasEvents)
            {
                return BadRequest("This event type cannot be deleted because it is used by one or more events.");
            }

            try
            {
                _context.EventTypes.Remove(eventType);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return BadRequest("This event type cannot be deleted because it is used by one or more events.");
            }
        }

        private async Task<bool> EventTypeExistsAsync(long id)
        {
            return await _context.EventTypes.AnyAsync(t => t.EventTypeId == id);
        }
    }
}
