using SmartEventPlatform.Contracts.Locations;
using SmartEventPlatform.Contracts.Speakers;

namespace SmartEventPlatform.EventService.Clients;

public interface IDirectoryServiceClient
{
    Task<LocationDto?> GetLocationAsync(long locationId);
    Task<SpeakerDto?> GetSpeakerAsync(long speakerId);
}