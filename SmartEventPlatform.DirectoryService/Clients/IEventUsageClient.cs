namespace SmartEventPlatform.DirectoryService.Clients;

public interface IEventUsageClient
{
    Task<bool> ExistsForLocationAsync(long locationId);
    Task<bool> ExistsForSpeakerAsync(long speakerId);
}