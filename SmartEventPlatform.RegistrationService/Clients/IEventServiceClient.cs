using SmartEventPlatform.Contracts.Events;

namespace SmartEventPlatform.RegistrationService.Clients;

public interface IEventServiceClient
{
    Task<List<EventDto>> GetAllEventsAsync();
    Task<EventRegistrationInfoDto> GetRegistrationInfoAsync(long eventId);
}