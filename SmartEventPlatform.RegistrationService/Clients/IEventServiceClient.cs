using SmartEventPlatform.Contracts.Events;

namespace SmartEventPlatform.RegistrationService.Clients;

public interface IEventServiceClient
{
    Task<List<EventDto>> GetAllEventsAsync();
    Task<EventRegistrationInfoDto> GetRegistrationInfoAsync(long eventId);

    Task<bool> ReserveSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken);

    Task ReleaseSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken);

    Task ConfirmSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken);
}