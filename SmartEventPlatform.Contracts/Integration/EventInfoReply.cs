namespace SmartEventPlatform.Contracts.Integration
{
    public class EventInfoReply
    {
        public long EventId { get; set; }
        public bool Exists { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }
}