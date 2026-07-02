using Microsoft.AspNetCore.Mvc;
using SmartEventPlatform.EventService.EventSourcing;

namespace SmartEventPlatform.EventService.Controllers
{
    /// <summary>
    /// API za upravljanje stručnim događajima kroz Event Sourcing.
    ///
    /// Svaki endpoint poziva odgovarajuću metodu na EventAggregate (koja generiše domenski događaj),
    /// a potom EventStoreRepository.SaveAsync() persistira taj događaj u bazu.
    ///
    /// Napomena: ovaj modul trenutno postoji kao samostalan kontroler, nije povezan
    /// sa API Gateway rutama niti sa Web frontend-om — testira se direktno preko Swaggera.
    /// </summary>
    [ApiController]
    [Route("api/eventsourced")]
    public class EventSourcedController : ControllerBase
    {
        private readonly EventStoreRepository _eventStore;
        private readonly ILogger<EventSourcedController> _logger;

        public EventSourcedController(
            EventStoreRepository eventStore,
            ILogger<EventSourcedController> logger)
        {
            _eventStore = eventStore;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventSourcedRequest req, CancellationToken ct)
        {
            _logger.LogInformation("EventSourcing: Kreiranje novog događaja '{Name}'", req.EventName);

            var aggregate = EventAggregate.Create(
                req.EventId,
                req.EventName,
                req.Agenda,
                req.EventDateTime,
                req.DurationInMinutes,
                req.RegistrationFee,
                req.LocationId,
                req.LocationName,
                req.EventTypeId);

            await _eventStore.SaveAsync(aggregate, ct);

            return Ok(ToResponse(aggregate));
        }


        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken ct)
        {
            var aggregate = await _eventStore.LoadAsync(id, ct);
            if (aggregate == null)
                return NotFound($"Event with ID={id} was not found in the event store.");

            return Ok(ToResponse(aggregate));
        }


        [HttpGet("{id:long}/history")]
        public async Task<IActionResult> GetHistory(long id, CancellationToken ct)
        {
            var history = await _eventStore.GetHistoryAsync(id, ct);

            if (!history.Any())
                return NotFound($"No history found for event with ID={id}.");

            return Ok(history);
        }


        [HttpPut("{id:long}/rename")]
        public async Task<IActionResult> Rename(long id, [FromBody] RenameEventRequest req, CancellationToken ct)
        {
            var aggregate = await _eventStore.LoadAsync(id, ct);
            if (aggregate == null) return NotFound();

            aggregate.Rename(req.NewName);
            await _eventStore.SaveAsync(aggregate, ct);

            _logger.LogInformation("EventSourcing: Preimenovan događaj {Id} → '{Name}'", id, req.NewName);
            return Ok(ToResponse(aggregate));
        }

        // ── PUT /api/eventsourced/{id}/reschedule ────────────────────────
        // Promijeni datum/vrijme i trajanje stručnog događaja

        [HttpPut("{id:long}/reschedule")]
        public async Task<IActionResult> Reschedule(long id, [FromBody] RescheduleEventRequest req, CancellationToken ct)
        {
            var aggregate = await _eventStore.LoadAsync(id, ct);
            if (aggregate == null) return NotFound();

            aggregate.Reschedule(req.NewDateTime, req.NewDurationInMinutes);
            await _eventStore.SaveAsync(aggregate, ct);

            _logger.LogInformation("EventSourcing: Rescheduled događaj {Id}", id);
            return Ok(ToResponse(aggregate));
        }

        // ── PUT /api/eventsourced/{id}/fee ───────────────────────────────
        // Promijeni cijenu kotizacije

        [HttpPut("{id:long}/fee")]
        public async Task<IActionResult> ChangeFee(long id, [FromBody] ChangeFeeRequest req, CancellationToken ct)
        {
            var aggregate = await _eventStore.LoadAsync(id, ct);
            if (aggregate == null) return NotFound();

            aggregate.ChangeFee(req.NewFee);
            await _eventStore.SaveAsync(aggregate, ct);

            _logger.LogInformation("EventSourcing: Promijenjena kotizacija za događaj {Id} → {Fee}", id, req.NewFee);
            return Ok(ToResponse(aggregate));
        }

        // ── PUT /api/eventsourced/{id}/location ──────────────────────────
        // Promijeni lokaciju stručnog događaja

        [HttpPut("{id:long}/location")]
        public async Task<IActionResult> ChangeLocation(long id, [FromBody] ChangeLocationRequest req, CancellationToken ct)
        {
            var aggregate = await _eventStore.LoadAsync(id, ct);
            if (aggregate == null) return NotFound();

            aggregate.ChangeLocation(req.NewLocationId, req.NewLocationName);
            await _eventStore.SaveAsync(aggregate, ct);

            _logger.LogInformation("EventSourcing: Promijenjena lokacija za događaj {Id}", id);
            return Ok(ToResponse(aggregate));
        }

        // ── POST /api/eventsourced/{id}/cancel ───────────────────────────
        // Otkaži stručni događaj (nema povratka — poslovno pravilo)

        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id, [FromBody] CancelEventRequest req, CancellationToken ct)
        {
            var aggregate = await _eventStore.LoadAsync(id, ct);
            if (aggregate == null) return NotFound();

            aggregate.Cancel(req.Reason);
            await _eventStore.SaveAsync(aggregate, ct);

            _logger.LogInformation("EventSourcing: Otkazan događaj {Id}. Razlog: {Reason}", id, req.Reason);
            return Ok(ToResponse(aggregate));
        }

        // ── POST /api/eventsourced/{id}/snapshot ─────────────────────────
        // Kreiraj snapshot trenutnog stanja (identično CreateBankAccountSnapshot() iz primjera)

        [HttpPost("{id:long}/snapshot")]
        public async Task<IActionResult> CreateSnapshot(long id, CancellationToken ct)
        {
            var aggregate = await _eventStore.LoadAsync(id, ct);
            if (aggregate == null) return NotFound();

            await _eventStore.CreateSnapshotAsync(aggregate, ct);

            _logger.LogInformation("EventSourcing: Kreiran snapshot za događaj {Id} na verziji {Version}",
                id, aggregate.Version);

            return Ok(new CreateSnapshotResponse
            {
                Message = $"Snapshot created at version {aggregate.Version}.",
                Version = aggregate.Version
            });
        }


        private static EventAggregateResponse ToResponse(EventAggregate a) => new()
        {
            EventId = a.Id,
            Version = a.Version,
            EventName = a.EventName,
            Agenda = a.Agenda,
            EventDateTime = a.EventDateTime,
            DurationInMinutes = a.DurationInMinutes,
            RegistrationFee = a.RegistrationFee,
            LocationId = a.LocationId,
            LocationName = a.LocationName,
            EventTypeId = a.EventTypeId,
            IsCancelled = a.IsCancelled,
            CancellationReason = a.CancellationReason
        };
    }
}