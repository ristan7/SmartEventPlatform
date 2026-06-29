using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.EventService.CQRS.Commands;
using SmartEventPlatform.EventService.CQRS.Queries;
using SmartEventPlatform.EventService.Data;

namespace SmartEventPlatform.EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        // ── CQRS Query handleri ──────────────────────────────────────────────
        private readonly GetAllEventsQueryHandler _getAllHandler;
        private readonly GetEventByIdQueryHandler _getByIdHandler;
        private readonly GetUpcomingEventsQueryHandler _getUpcomingHandler;

        // ── CQRS Command handleri ────────────────────────────────────────────
        private readonly CreateEventCommandHandler _createHandler;
        private readonly UpdateEventCommandHandler _updateHandler;
        private readonly DeleteEventCommandHandler _deleteHandler;

        // ── Direktni DbContext ostaje samo za pomoćne endpoint-e koji nisu
        //    user-facing CRUD operacije (exists-for-location, registration-info…)
        private readonly EventDbContext _context;

        public EventsController(
            GetAllEventsQueryHandler getAllHandler,
            GetEventByIdQueryHandler getByIdHandler,
            GetUpcomingEventsQueryHandler getUpcomingHandler,
            CreateEventCommandHandler createHandler,
            UpdateEventCommandHandler updateHandler,
            DeleteEventCommandHandler deleteHandler,
            EventDbContext context)
        {
            _getAllHandler = getAllHandler;
            _getByIdHandler = getByIdHandler;
            _getUpcomingHandler = getUpcomingHandler;
            _createHandler = createHandler;
            _updateHandler = updateHandler;
            _deleteHandler = deleteHandler;
            _context = context;
        }

        // ══════════════════════════════════════════════════════════════
        //  QUERY endpoint-i — čitanje, CQRS Query strana
        // ══════════════════════════════════════════════════════════════

        /// <summary>Vraća sve događaje. Poziva GetAllEventsQueryHandler.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetAll()
        {
            var results = await _getAllHandler.Handle(new GetAllEventsQuery());

            var dtos = results.Select(r => new EventDto
            {
                EventId = r.EventId,
                EventName = r.EventName,
                Agenda = r.Agenda,
                EventDateTime = r.EventDateTime,
                DurationInMinutes = r.DurationInMinutes,
                RegistrationFee = r.RegistrationFee,
                LocationId = r.LocationId,
                LocationName = r.LocationName,
                LocationAddress = r.LocationAddress,
                Capacity = r.Capacity,
                EventTypeId = r.EventTypeId,
                EventTypeName = r.EventTypeName,
                Speakers = r.Speakers
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>Vraća jedan događaj po ID-u. Poziva GetEventByIdQueryHandler.</summary>
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EventDto>> GetById(long id)
        {
            var result = await _getByIdHandler.Handle(new GetEventByIdQuery { EventId = id });

            if (result == null)
                return NotFound();

            return Ok(new EventDto
            {
                EventId = result.EventId,
                EventName = result.EventName,
                Agenda = result.Agenda,
                EventDateTime = result.EventDateTime,
                DurationInMinutes = result.DurationInMinutes,
                RegistrationFee = result.RegistrationFee,
                LocationId = result.LocationId,
                LocationName = result.LocationName,
                LocationAddress = result.LocationAddress,
                Capacity = result.Capacity,
                EventTypeId = result.EventTypeId,
                EventTypeName = result.EventTypeName,
                Speakers = result.Speakers
            });
        }

        /// <summary>
        /// Vraća predstojeće događaje filtrirane po datumu. Poziva GetUpcomingEventsQueryHandler.
        /// Primjer: GET /api/events/upcoming?fromDate=2026-07-01
        /// Bez fromDate parametra vraća od danas.
        /// </summary>
        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetUpcoming([FromQuery] DateTime? fromDate)
        {
            var results = await _getUpcomingHandler.Handle(
                new GetUpcomingEventsQuery { FromDate = fromDate });

            var dtos = results.Select(r => new EventDto
            {
                EventId = r.EventId,
                EventName = r.EventName,
                Agenda = r.Agenda,
                EventDateTime = r.EventDateTime,
                DurationInMinutes = r.DurationInMinutes,
                RegistrationFee = r.RegistrationFee,
                LocationId = r.LocationId,
                LocationName = r.LocationName,
                LocationAddress = r.LocationAddress,
                Capacity = r.Capacity,
                EventTypeId = r.EventTypeId,
                EventTypeName = r.EventTypeName,
                Speakers = r.Speakers
            }).ToList();

            return Ok(dtos);
        }

        // ══════════════════════════════════════════════════════════════
        //  COMMAND endpoint-i — izmjena stanja, CQRS Command strana
        // ══════════════════════════════════════════════════════════════

        /// <summary>Kreira novi događaj. Poziva CreateEventCommandHandler.</summary>
        [HttpPost]
        public async Task<ActionResult<long>> Create(EventCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var newId = await _createHandler.Handle(new CreateEventCommand
                {
                    EventName = dto.EventName,
                    Agenda = dto.Agenda,
                    EventDateTime = dto.EventDateTime,
                    DurationInMinutes = dto.DurationInMinutes,
                    RegistrationFee = dto.RegistrationFee,
                    LocationId = dto.LocationId,
                    EventTypeId = dto.EventTypeId
                });

                return CreatedAtAction(nameof(GetById), new { id = newId }, newId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Ažurira događaj. Poziva UpdateEventCommandHandler.</summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, EventCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var found = await _updateHandler.Handle(new UpdateEventCommand
                {
                    EventId = id,
                    EventName = dto.EventName,
                    Agenda = dto.Agenda,
                    EventDateTime = dto.EventDateTime,
                    DurationInMinutes = dto.DurationInMinutes,
                    RegistrationFee = dto.RegistrationFee,
                    LocationId = dto.LocationId,
                    EventTypeId = dto.EventTypeId
                });

                if (!found)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Briše događaj. Poziva DeleteEventCommandHandler.</summary>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var found = await _deleteHandler.Handle(new DeleteEventCommand { EventId = id });

                if (!found)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  Pomoćni endpoint-i za inter-service komunikaciju
        //  Ostaju s direktnim DbContext-om — nisu user CRUD operacije.
        // ══════════════════════════════════════════════════════════════

        [HttpGet("exists-for-location/{locationId:long}")]
        public async Task<ActionResult<bool>> ExistsForLocation(long locationId)
        {
            var exists = await _context.Events.AnyAsync(e => e.LocationId == locationId);
            return Ok(exists);
        }

        [HttpGet("exists-for-speaker/{speakerId:long}")]
        public async Task<ActionResult<bool>> ExistsForSpeaker(long speakerId)
        {
            var exists = await _context.EventSpeakers.AnyAsync(es => es.SpeakerId == speakerId);
            return Ok(exists);
        }

        [HttpGet("{id:long}/registration-info")]
        public async Task<ActionResult<EventRegistrationInfoDto>> GetRegistrationInfo(long id)
        {
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
                return Ok(new EventRegistrationInfoDto { EventId = id, Exists = false });

            return Ok(dto);
        }

        [HttpGet("{id:long}/delete-info")]
        public async Task<ActionResult<EventDto>> GetDeleteInfo(long id)
        {
            var result = await _getByIdHandler.Handle(new GetEventByIdQuery { EventId = id });

            if (result == null)
                return NotFound();

            return Ok(new EventDto
            {
                EventId = result.EventId,
                EventName = result.EventName,
                Agenda = result.Agenda,
                EventDateTime = result.EventDateTime,
                DurationInMinutes = result.DurationInMinutes,
                RegistrationFee = result.RegistrationFee,
                LocationId = result.LocationId,
                LocationName = result.LocationName,
                LocationAddress = result.LocationAddress,
                Capacity = result.Capacity,
                EventTypeId = result.EventTypeId,
                EventTypeName = result.EventTypeName
            });
        }
    }
}