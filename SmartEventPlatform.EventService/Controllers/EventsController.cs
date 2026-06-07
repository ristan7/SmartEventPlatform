using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Services;
using SmartEventPlatformWeb.EventService.Models;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly EventDbContext _context;
        private readonly IRegistrationServiceClient _registrationServiceClient;

        //private static int _counter = 0;

        public EventsController(EventDbContext context, IRegistrationServiceClient registrationServiceClient)
        {
            _context = context;
            _registrationServiceClient = registrationServiceClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetAll()
        {
            var events = await _context.Events
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .Include(e => e.EventSpeakers)
                    .ThenInclude(es => es.Speaker)
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
                    LocationName = e.Location != null ? e.Location.LocationName : string.Empty,
                    LocationAddress = e.Location != null ? e.Location.Address : string.Empty,
                    Capacity = e.Location != null ? e.Location.Capacity : 0,

                    EventTypeId = e.EventTypeId,
                    EventTypeName = e.EventType != null ? e.EventType.Name : string.Empty,

                    Speakers = e.EventSpeakers
                        .OrderBy(es => es.Time)
                        .Select(es => es.Speaker != null
                            ? es.Speaker.FirstName + " " + es.Speaker.LastName
                            : string.Empty)
                        .ToList()
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<EventDto>> GetById(long id)
        {
            var eventDto = await _context.Events
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .Include(e => e.EventSpeakers)
                    .ThenInclude(es => es.Speaker)
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
                    LocationName = e.Location != null ? e.Location.LocationName : string.Empty,
                    LocationAddress = e.Location != null ? e.Location.Address : string.Empty,
                    Capacity = e.Location != null ? e.Location.Capacity : 0,

                    EventTypeId = e.EventTypeId,
                    EventTypeName = e.EventType != null ? e.EventType.Name : string.Empty,

                    Speakers = e.EventSpeakers
                        .OrderBy(es => es.Time)
                        .Select(es => es.Speaker != null
                            ? es.Speaker.FirstName + " " + es.Speaker.LastName
                            : string.Empty)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (eventDto == null)
            {
                return NotFound();
            }

            return Ok(eventDto);
        }

        [HttpGet("{id:long}/registration-info")]
        public async Task<ActionResult<EventRegistrationInfoDto>> GetRegistrationInfo(long id)
        {
            //_counter++;

            //if (_counter % 3 != 0)
            //{
            //    return StatusCode(500, "Simulated temporary EventService error.");
            //}

            //await Task.Delay(10000);

            //return StatusCode(500, "Simulated EventService failure.");

            var dto = await _context.Events
                .Include(e => e.Location)
                .Where(e => e.EventId == id)
                .Select(e => new EventRegistrationInfoDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDateTime = e.EventDateTime,
                    Capacity = e.Location != null ? e.Location.Capacity : 0,
                    Exists = true
                })
                .FirstOrDefaultAsync();

            if (dto == null)
            {
                return NotFound(new EventRegistrationInfoDto
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
            {
                return ValidationProblem(ModelState);
            }

            var locationExists = await _context.Locations
                .AnyAsync(l => l.LocationId == dto.LocationId);

            if (!locationExists)
            {
                return BadRequest("Selected location does not exist.");
            }

            var eventTypeExists = await _context.EventTypes
                .AnyAsync(et => et.EventTypeId == dto.EventTypeId);

            if (!eventTypeExists)
            {
                return BadRequest("Selected event type does not exist.");
            }

            var newEvent = new Event
            {
                EventName = dto.EventName,
                Agenda = dto.Agenda,
                EventDateTime = dto.EventDateTime,
                DurationInMinutes = dto.DurationInMinutes,
                RegistrationFee = dto.RegistrationFee,
                LocationId = dto.LocationId,
                EventTypeId = dto.EventTypeId
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = newEvent.EventId }, newEvent.EventId);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, EventCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            var locationExists = await _context.Locations
                .AnyAsync(l => l.LocationId == dto.LocationId);

            if (!locationExists)
            {
                return BadRequest("Selected location does not exist.");
            }

            var eventTypeExists = await _context.EventTypes
                .AnyAsync(et => et.EventTypeId == dto.EventTypeId);

            if (!eventTypeExists)
            {
                return BadRequest("Selected event type does not exist.");
            }

            existingEvent.EventName = dto.EventName;
            existingEvent.Agenda = dto.Agenda;
            existingEvent.EventDateTime = dto.EventDateTime;
            existingEvent.DurationInMinutes = dto.DurationInMinutes;
            existingEvent.RegistrationFee = dto.RegistrationFee;
            existingEvent.LocationId = dto.LocationId;
            existingEvent.EventTypeId = dto.EventTypeId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await EventExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<EventDto>> GetDeleteInfo(long id)
        {
            var eventDto = await _context.Events
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .Where(e => e.EventId == id)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDateTime = e.EventDateTime,
                    LocationId = e.LocationId,
                    LocationName = e.Location != null ? e.Location.LocationName : string.Empty,
                    EventTypeId = e.EventTypeId,
                    EventTypeName = e.EventType != null ? e.EventType.Name : string.Empty,
                    Agenda = e.Agenda,
                    DurationInMinutes = e.DurationInMinutes,
                    RegistrationFee = e.RegistrationFee
                })
                .FirstOrDefaultAsync();

            if (eventDto == null)
            {
                return NotFound();
            }

            return Ok(eventDto);
        }


        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existingEvent = await _context.Events
                .Include(e => e.Location)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            var hasAssignedSpeakers = await _context.EventSpeakers
                .AnyAsync(es => es.EventId == id);

            if (hasAssignedSpeakers)
            {
                return BadRequest("This event cannot be deleted because it has assigned speakers.");
            }

            var hasRegistrations = await _registrationServiceClient.EventHasRegistrationsAsync(id);

            if (hasRegistrations)
            {
                return BadRequest("This event cannot be deleted because it has participant registrations.");
            }

            try
            {
                _context.Events.Remove(existingEvent);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return BadRequest("This event cannot be deleted because it is used by other records.");
            }
        }

        private async Task<bool> EventExistsAsync(long id)
        {
            return await _context.Events.AnyAsync(e => e.EventId == id);
        }
    }
}
