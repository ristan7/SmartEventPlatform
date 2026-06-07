using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.EventSpeakers;
using SmartEventPlatform.Contracts.EventTypes;
using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatform.Contracts.Speakers;

namespace SmartEventPlatformWeb.Services;

public interface IEventApiClient
{
    Task<List<EventDto>> GetEventsAsync();
    Task<EventDto?> GetEventByIdAsync(long id);
    Task<EventRegistrationInfoDto?> GetEventRegistrationInfoAsync(long id);
    Task<long> CreateEventAsync(EventCreateUpdateDto dto);
    Task UpdateEventAsync(long id, EventCreateUpdateDto dto);
    Task DeleteEventAsync(long id);

    Task<List<LocationDto>> GetLocationsAsync();
    Task<LocationDto?> GetLocationByIdAsync(long id);
    Task<long> CreateLocationAsync(LocationDto dto);
    Task UpdateLocationAsync(long id, LocationDto dto);
    Task DeleteLocationAsync(long id);

    Task<List<EventTypeDto>> GetEventTypesAsync();
    Task<EventTypeDto?> GetEventTypeByIdAsync(long id);
    Task<long> CreateEventTypeAsync(EventTypeDto dto);
    Task UpdateEventTypeAsync(long id, EventTypeDto dto);
    Task DeleteEventTypeAsync(long id);

    Task<List<SpeakerDto>> GetSpeakersAsync();
    Task<SpeakerDto?> GetSpeakerByIdAsync(long id);
    Task<long> CreateSpeakerAsync(SpeakerDto dto);
    Task UpdateSpeakerAsync(long id, SpeakerDto dto);
    Task DeleteSpeakerAsync(long id);

    Task<List<EventSpeakerDto>> GetEventSpeakersAsync();
    Task<EventSpeakerDto?> GetEventSpeakerByIdAsync(long id);
    Task<long> CreateEventSpeakerAsync(EventSpeakerCreateUpdateDto dto);
    Task UpdateEventSpeakerAsync(long id, EventSpeakerCreateUpdateDto dto);
    Task DeleteEventSpeakerAsync(long id);
}