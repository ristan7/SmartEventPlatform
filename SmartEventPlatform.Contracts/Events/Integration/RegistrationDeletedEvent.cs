namespace SmartEventPlatform.Contracts.Events.Integration
{
    public class RegistrationDeletedEvent
    {
        public long RegistrationId { get; set; }
        public long EventId { get; set; }
    }
}