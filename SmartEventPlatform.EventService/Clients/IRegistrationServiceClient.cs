namespace SmartEventPlatform.EventService.Clients;

public interface IRegistrationServiceClient
{
    Task<bool> EventHasRegistrationsAsync(long eventId);
}