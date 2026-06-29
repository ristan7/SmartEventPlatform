using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.DirectoryService.Data;
using SmartEventPlatform.DirectoryService.Models;

namespace SmartEventPlatform.DirectoryService.Controllers;

/// <summary>
/// Saga endpointi za DirectoryService.
///
/// Ove rute poziva SAMO RegistrationSagaOrchestrator - nisu za direktnu upotrebu.
///
/// Tok:
///   POST  record-attendance  → Korak 3: Zabilježi jednu registraciju na lokaciji
///   DELETE release-attendance → Korak 3 kompenzacija: Poništi evidenciju
/// </summary>
[ApiController]
[Route("api/saga")]
public class SagaController : ControllerBase
{
    private readonly DirectoryDbContext _context;
    private readonly ILogger<SagaController> _logger;

    public SagaController(DirectoryDbContext context, ILogger<SagaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Korak 3 Sage: Incrementiraj brojač registracija za lokaciju.
    ///
    /// Ako lokacija ne postoji u bazi vraća 404.
    /// Ako LocationRegistrationTracker za ovu lokaciju već postoji, incrementira ga.
    /// Ako ne postoji, kreira novi s vrijednosti 1.
    ///
    /// Idempotentno: sagaId se koristi samo za logging jer je DirectoryService
    /// stateless po pitanju SagaId-a (ne čuva koje sage su registrovane).
    /// </summary>
    [HttpPost("locations/{locationId:long}/record-attendance")]
    public async Task<IActionResult> RecordAttendance(long locationId, [FromQuery] long sagaId)
    {
        _logger.LogInformation(
            "[DirectoryService Saga] RecordAttendance pozvan: LocationId={LocationId}, SagaId={SagaId}.",
            locationId, sagaId);

        // Provjeri da li lokacija postoji
        var locationExists = await _context.Locations.AnyAsync(l => l.LocationId == locationId);
        if (!locationExists)
        {
            _logger.LogWarning(
                "[DirectoryService Saga] Lokacija {LocationId} ne postoji.", locationId);
            return NotFound($"Location {locationId} not found.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var tracker = await _context.LocationRegistrationTrackers
                .FirstOrDefaultAsync(t => t.LocationId == locationId);

            if (tracker == null)
            {
                // Prva registracija za ovu lokaciju
                _context.LocationRegistrationTrackers.Add(new LocationRegistrationTracker
                {
                    LocationId = locationId,
                    RegistrationCount = 1
                });
                _logger.LogInformation(
                    "[DirectoryService Saga] LocationRegistrationTracker kreiran za LocationId={LocationId}. Count=1.",
                    locationId);
            }
            else
            {
                tracker.RegistrationCount++;
                _logger.LogInformation(
                    "[DirectoryService Saga] LocationRegistrationTracker incrementiran za LocationId={LocationId}. " +
                    "Novi Count={Count}.",
                    locationId, tracker.RegistrationCount);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "[DirectoryService Saga] RecordAttendance završen: LocationId={LocationId}, SagaId={SagaId}.",
                locationId, sagaId);

            return Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex,
                "[DirectoryService Saga] RecordAttendance pukao: LocationId={LocationId}, SagaId={SagaId}.",
                locationId, sagaId);
            throw;
        }
    }

    /// <summary>
    /// Korak 3 kompenzacija: Smanji brojač registracija za lokaciju.
    ///
    /// Poziva se kada Korak 4 Sage ne uspije.
    /// Ako tracker ne postoji ili je već 0, vraća 404 (kompenzacija ignoruje ovu grešku).
    /// </summary>
    [HttpDelete("locations/{locationId:long}/release-attendance")]
    public async Task<IActionResult> ReleaseAttendance(long locationId, [FromQuery] long sagaId)
    {
        _logger.LogInformation(
            "[DirectoryService Saga] ReleaseAttendance (kompenzacija) pozvan: LocationId={LocationId}, SagaId={SagaId}.",
            locationId, sagaId);

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var tracker = await _context.LocationRegistrationTrackers
                .FirstOrDefaultAsync(t => t.LocationId == locationId);

            if (tracker == null || tracker.RegistrationCount <= 0)
            {
                _logger.LogWarning(
                    "[DirectoryService Saga] LocationRegistrationTracker za LocationId={LocationId} nije pronađen " +
                    "ili je već na 0. Kompenzacija možda već izvršena.",
                    locationId);
                await tx.CommitAsync();
                return NotFound();
            }

            tracker.RegistrationCount--;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "[DirectoryService Saga] ReleaseAttendance završen: LocationId={LocationId}, SagaId={SagaId}. " +
                "Novi Count={Count}.",
                locationId, sagaId, tracker.RegistrationCount);

            return Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex,
                "[DirectoryService Saga] ReleaseAttendance pukao: LocationId={LocationId}, SagaId={SagaId}.",
                locationId, sagaId);
            throw;
        }
    }
}