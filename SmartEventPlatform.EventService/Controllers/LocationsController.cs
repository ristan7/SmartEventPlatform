using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly EventDbContext _context;

        public LocationsController(EventDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll()
        {
            var locations = await _context.Locations
                .OrderBy(l => l.LocationName)
                .Select(l => new LocationDto
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    Address = l.Address,
                    Capacity = l.Capacity
                })
                .ToListAsync();

            return Ok(locations);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<LocationDto>> GetById(long id)
        {
            var location = await _context.Locations
                .Where(l => l.LocationId == id)
                .Select(l => new LocationDto
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    Address = l.Address,
                    Capacity = l.Capacity
                })
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound();
            }

            return Ok(location);
        }

        [HttpPost]
        public async Task<ActionResult<long>> Create(LocationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var location = new Location
            {
                LocationName = dto.LocationName,
                Address = dto.Address,
                Capacity = dto.Capacity
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = location.LocationId }, location.LocationId);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, LocationDto dto)
        {
            if (id != dto.LocationId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var location = await _context.Locations.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            location.LocationName = dto.LocationName;
            location.Address = dto.Address;
            location.Capacity = dto.Capacity;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await LocationExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<LocationDto>> GetDeleteInfo(long id)
        {
            var location = await _context.Locations
                .Where(l => l.LocationId == id)
                .Select(l => new LocationDto
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    Address = l.Address,
                    Capacity = l.Capacity
                })
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound();
            }

            return Ok(location);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var location = await _context.Locations.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            var hasEvents = await _context.Events
                .AnyAsync(e => e.LocationId == id);

            if (hasEvents)
            {
                return BadRequest("This location cannot be deleted because it is used by one or more events.");
            }

            try
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return BadRequest("This location cannot be deleted because it is used by one or more events.");
            }
        }

        private async Task<bool> LocationExistsAsync(long id)
        {
            return await _context.Locations.AnyAsync(l => l.LocationId == id);
        }
    }
}
