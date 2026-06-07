using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.Contracts.Participants;
using SmartEventPlatform.Contracts.Registrations;

namespace SmartEventPlatformWeb.Services;

public interface IRegistrationApiClient
{
    Task<List<ParticipantDto>> GetParticipantsAsync();
    Task<ParticipantDto?> GetParticipantByIdAsync(long id);
    Task<long> CreateParticipantAsync(ParticipantDto dto);
    Task UpdateParticipantAsync(long id, ParticipantDto dto);
    Task DeleteParticipantAsync(long id);

    Task<List<RegistrationDto>> GetRegistrationsAsync();
    Task<RegistrationDto?> GetRegistrationByIdAsync(long id);
    Task<long> CreateRegistrationAsync(RegistrationCreateUpdateDto dto);
    Task UpdateRegistrationAsync(long id, RegistrationCreateUpdateDto dto);
    Task DeleteRegistrationAsync(long id);

    Task<List<AvailableEventDto>> GetAvailableEventsAsync();

    Task<bool> ExistsForEventAsync(long eventId);
}