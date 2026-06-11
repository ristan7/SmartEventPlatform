using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.EventService.Clients;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Models;
using System.Diagnostics.Metrics;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly EventDbContext _context;
        private readonly IDirectoryServiceClient _directoryServiceClient;
        private readonly IRegistrationServiceClient _registrationServiceClient;

        //private static int _counter = 0;

        public EventsController(
            EventDbContext context,
            IDirectoryServiceClient directoryServiceClient,
            IRegistrationServiceClient registrationServiceClient)
        {
            _context = context;
            _directoryServiceClient = directoryServiceClient;
            _registrationServiceClient = registrationServiceClient;
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
            {
                return NotFound();
            }

            return Ok(eventDto);
        }

        [HttpGet("{id:long}/registration-info")]
        public async Task<ActionResult<EventRegistrationInfoDto>> GetRegistrationInfo(long id)
        {

            //var attempt = Interlocked.Increment(ref _counter);

            //if (attempt % 3 != 0)
            //{
            //    return StatusCode(500, "Simulated temporary EventService error.");
            //}

            //await Task.Delay(10000);

            //return StatusCode(500, "Simulated EventService failure.");

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
            {
                return ValidationProblem(ModelState);
            }

            var location = await _directoryServiceClient.GetLocationAsync(dto.LocationId);

            if (location == null)
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
                LocationNameSnapshot = location.LocationName,
                LocationAddressSnapshot = location.Address,
                LocationCapacitySnapshot = location.Capacity,
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

            var location = await _directoryServiceClient.GetLocationAsync(dto.LocationId);

            if (location == null)
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
            existingEvent.LocationNameSnapshot = location.LocationName;
            existingEvent.LocationAddressSnapshot = location.Address;
            existingEvent.LocationCapacitySnapshot = location.Capacity;
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
            {
                return NotFound();
            }

            return Ok(eventDto);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var existingEvent = await _context.Events
                .Include(e => e.EventSpeakers)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            var deleteErrors = new List<string>();

            var hasAssignedSpeakers = existingEvent.EventSpeakers.Any();

            if (hasAssignedSpeakers)
            {
                deleteErrors.Add("This event cannot be deleted because it has assigned speakers.");
            }

            var hasRegistrations = await _registrationServiceClient.EventHasRegistrationsAsync(id);

            if (hasRegistrations)
            {
                deleteErrors.Add("This event cannot be deleted because it has participant registrations.");
            }

            if (deleteErrors.Any())
            {
                return BadRequest(string.Join(" ", deleteErrors));
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