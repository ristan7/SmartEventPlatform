using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Models;

namespace SmartEventPlatform.EventService.Controllers;


[ApiController]
[Route("api/saga")]
public class SagaController : ControllerBase
{
    private readonly EventDbContext _context;
    private readonly ILogger<SagaController> _logger;

    public SagaController(EventDbContext context, ILogger<SagaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    
    [HttpPost("events/{eventId:long}/reserve-spot")]
    public async Task<IActionResult> ReserveSpot(long eventId, [FromQuery] long sagaId)
    {
        _logger.LogInformation(
            "[EventService Saga] ReserveSpot pozvan: EventId={EventId}, SagaId={SagaId}.",
            eventId, sagaId);

        // Proveri da li događaj postoji
        var eventEntity = await _context.Events.FindAsync(eventId);
        if (eventEntity == null)
        {
            _logger.LogWarning("[EventService Saga] Događaj {EventId} ne postoji.", eventId);
            return NotFound($"Event {eventId} not found.");
        }

        // Proveri da li ova Saga već ima rezervaciju (idempotentnost)
        var existingReservation = await _context.SagaSpotReservations
            .FirstOrDefaultAsync(r => r.SagaId == sagaId);

        if (existingReservation != null)
        {
            _logger.LogInformation(
                "[EventService Saga] Saga {SagaId} već ima rezervaciju za EventId={EventId}. Idempotentno vraćamo 200.",
                sagaId, eventId);
            return Ok();
        }

        // Proveri kapacitet: potvrđene + privremene rezervacije
        var confirmedCount = await _context.EventRegistrationTrackers
            .Where(t => t.EventId == eventId)
            .Select(t => t.RegistrationCount)
            .FirstOrDefaultAsync();

        var pendingReservations = await _context.SagaSpotReservations
            .CountAsync(r => r.EventId == eventId);

        var totalOccupied = confirmedCount + pendingReservations;

        _logger.LogInformation(
            "[EventService Saga] EventId={EventId}: Kapacitet={Capacity}, Potvrđeni={Confirmed}, " +
            "PendingRezervacije={Pending}, Ukupno={Total}.",
            eventId, eventEntity.LocationCapacitySnapshot, confirmedCount, pendingReservations, totalOccupied);

        if (totalOccupied >= eventEntity.LocationCapacitySnapshot)
        {
            _logger.LogWarning(
                "[EventService Saga] EventId={EventId} nema slobodnih mesta " +
                "(kapacitet={Cap}, zauzeto={Total}).",
                eventId, eventEntity.LocationCapacitySnapshot, totalOccupied);
            return Conflict("No available spots for this event.");
        }

        // Kreiraj privremenu rezervaciju
        _context.SagaSpotReservations.Add(new SagaSpotReservation
        {
            SagaId = sagaId,
            EventId = eventId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[EventService Saga] Rezervacija kreirana: SagaId={SagaId}, EventId={EventId}.",
            sagaId, eventId);

        return Ok();
    }

    
    [HttpPost("events/{eventId:long}/confirm-spot")]
    public async Task<IActionResult> ConfirmSpot(long eventId, [FromQuery] long sagaId)
    {
        _logger.LogInformation(
            "[EventService Saga] ConfirmSpot pozvan: EventId={EventId}, SagaId={SagaId}.",
            eventId, sagaId);

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Pronađi privremenu rezervaciju
            var reservation = await _context.SagaSpotReservations
                .FirstOrDefaultAsync(r => r.SagaId == sagaId && r.EventId == eventId);

            if (reservation == null)
            {
                // Može se desiti pri ponovnom pokušaju (idempotentnost)
                _logger.LogWarning(
                    "[EventService Saga] Nije pronađena rezervacija za SagaId={SagaId}, EventId={EventId}. " +
                    "Možda je već potvrđena - nastavljamo.",
                    sagaId, eventId);
                await tx.CommitAsync();
                return Ok();
            }

            // Ukloni privremenu rezervaciju
            _context.SagaSpotReservations.Remove(reservation);

            // Incrementiraj EventRegistrationTracker
            var tracker = await _context.EventRegistrationTrackers.FindAsync(eventId);
            if (tracker == null)
            {
                _context.EventRegistrationTrackers.Add(new EventRegistrationTracker
                {
                    EventId = eventId,
                    RegistrationCount = 1
                });
            }
            else
            {
                tracker.RegistrationCount++;
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "[EventService Saga] ConfirmSpot završen: SagaId={SagaId}, EventId={EventId}. " +
                "EventRegistrationTracker incrementiran.",
                sagaId, eventId);

            return Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex,
                "[EventService Saga] ConfirmSpot pukao: SagaId={SagaId}, EventId={EventId}.",
                sagaId, eventId);
            throw;
        }
    }

    
    [HttpDelete("events/{eventId:long}/release-spot")]
    public async Task<IActionResult> ReleaseSpot(long eventId, [FromQuery] long sagaId)
    {
        _logger.LogInformation(
            "[EventService Saga] ReleaseSpot (kompenzacija) pozvan: EventId={EventId}, SagaId={SagaId}.",
            eventId, sagaId);

        var reservation = await _context.SagaSpotReservations
            .FirstOrDefaultAsync(r => r.SagaId == sagaId && r.EventId == eventId);

        if (reservation == null)
        {
            _logger.LogWarning(
                "[EventService Saga] Rezervacija nije pronađena za SagaId={SagaId}, EventId={EventId}. " +
                "Kompenzacija već izvršena ili rezervacija nikad nije napravljena.",
                sagaId, eventId);
            return NotFound();
        }

        _context.SagaSpotReservations.Remove(reservation);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[EventService Saga] Rezervacija otkazana (kompenzacija): SagaId={SagaId}, EventId={EventId}.",
            sagaId, eventId);

        return Ok();
    }
}