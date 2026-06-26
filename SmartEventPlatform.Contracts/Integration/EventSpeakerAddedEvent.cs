namespace SmartEventPlatform.Contracts.Integration
{
    public class EventSpeakerAddedEvent
    {
        public long EventSpeakerId { get; set; }
        public long SpeakerId { get; set; }
    }
}