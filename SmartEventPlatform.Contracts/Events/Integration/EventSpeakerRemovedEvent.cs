namespace SmartEventPlatform.Contracts.Events.Integration
{
    public class EventSpeakerRemovedEvent
    {
        public long EventSpeakerId { get; set; }
        public long SpeakerId { get; set; }
    }
}