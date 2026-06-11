using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.RegistrationService.Clients;
using SmartEventPlatform.RegistrationService.Data;

namespace SmartEventPlatform.RegistrationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailableEventsController : ControllerBase
{
    private readonly RegistrationDbContext _context;
    private readonly IEventServiceClient _eventServiceClient;

    public AvailableEventsController(RegistrationDbContext context, IEventServiceClient eventServiceClient)
    {
        _context = context;
        _eventServiceClient = eventServiceClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvailableEventDto>>> GetAvailableEvents()
    {

        var events = await _eventServiceClient.GetAllEventsAsync();

        var now = DateTime.Now;

        var futureEvents = events
            .Where(e => e.EventDateTime >= now)
            .ToList();

        var eventIds = futureEvents
            .Select(e => e.EventId)
            .ToList();

        var registrationCounts = await _context.Registrations
            .Where(r => eventIds.Contains(r.EventId))
            .GroupBy(r => r.EventId)
            .Select(g => new
            {
                EventId = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var availableEvents = futureEvents
            .Select(e =>
            {
                var registeredCount = registrationCounts
                    .FirstOrDefault(rc => rc.EventId == e.EventId)?.Count ?? 0;

                return new AvailableEventDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Agenda = e.Agenda,
                    EventDateTime = e.EventDateTime,
                    DurationInMinutes = e.DurationInMinutes,
                    RegistrationFee = e.RegistrationFee,
                    LocationName = e.LocationName,
                    Capacity = e.Capacity,
                    RegisteredCount = registeredCount,
                    Speakers = e.Speakers
                };
            })
            .Where(e => e.RegisteredCount < e.Capacity)
            .OrderBy(e => e.EventDateTime)
            .ToList();

        return Ok(availableEvents);

    }
}