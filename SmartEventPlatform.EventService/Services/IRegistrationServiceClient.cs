namespace SmartEventPlatform.EventService.Services
{
    public interface IRegistrationServiceClient
    {
        Task<bool> EventHasRegistrationsAsync(long eventId);
    }
}
