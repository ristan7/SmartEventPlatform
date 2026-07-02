namespace SmartEventPlatform.EventService.Models
{
    public class ProcessedMessage
    {
        public long Id { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime ProcessedAtUtc { get; set; }
    }
}