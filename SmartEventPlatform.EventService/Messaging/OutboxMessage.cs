namespace SmartEventPlatform.EventService.Messaging
{
    public class OutboxMessage
    {
        public long Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}