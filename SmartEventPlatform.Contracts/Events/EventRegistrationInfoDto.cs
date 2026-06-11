namespace SmartEventPlatform.Contracts.Events
{
    public class EventRegistrationInfoDto
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int Capacity { get; set; }
        public bool Exists { get; set; }
    }
}
