using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.DirectoryService.Data;
using SmartEventPlatform.DirectoryService.Models;

namespace SmartEventPlatform.DirectoryService.Controllers;


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

    
    [HttpPost("locations/{locationId:long}/record-attendance")]
    public async Task<IActionResult> RecordAttendance(long locationId, [FromQuery] long sagaId)
    {
        _logger.LogInformation(
            "[DirectoryService Saga] RecordAttendance pozvan: LocationId={LocationId}, SagaId={SagaId}.",
            locationId, sagaId);

        // Proveri da li lokacija postoji
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