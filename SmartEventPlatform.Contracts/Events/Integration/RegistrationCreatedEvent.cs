namespace SmartEventPlatform.Contracts.Events.Integration
{
    public class RegistrationCreatedEvent
    {
        public long RegistrationId { get; set; }
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}