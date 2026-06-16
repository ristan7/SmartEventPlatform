namespace SmartEventPlatform.Contracts.Events.Integration
{
    public class EventSpeakerAddedEvent
    {
        public long EventSpeakerId { get; set; }
        public long SpeakerId { get; set; }
    }
}