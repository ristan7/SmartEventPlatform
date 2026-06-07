using SmartEventPlatform.Contracts.Events;

namespace SmartEventPlatform.RegistrationService.Services
{
    public interface IEventServiceClient
    {
        Task<EventRegistrationInfoDto?> GetRegistrationInfoAsync(long eventId);
        Task<List<EventDto>> GetEventsAsync();
    }
}
