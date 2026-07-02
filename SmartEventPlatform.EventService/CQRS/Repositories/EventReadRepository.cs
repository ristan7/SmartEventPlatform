using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.EventService.Data;

namespace SmartEventPlatform.EventService.CQRS.Repositories
{
    public class EventReadRepository : IEventReadRepository
    {
        private readonly EventDbContext _context;

        public EventReadRepository(EventDbContext context)
        {
            _context = context;
        }

        public async Task<List<EventDto>> GetAllAsync()
        {
            return await _context.Events
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
        }

        public async Task<EventDto?> GetByIdAsync(long id)
        {
            return await _context.Events
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
        }

        public async Task<List<EventDto>> GetUpcomingAsync(DateTime fromDate)
        {
            return await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.EventSpeakers)
                .Where(e => e.EventDateTime >= fromDate)
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
        }
    }
}