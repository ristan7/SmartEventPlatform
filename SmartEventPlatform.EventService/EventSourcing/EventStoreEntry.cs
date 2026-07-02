namespace SmartEventPlatform.EventService.EventSourcing
{
    
    public class EventStoreEntry
    {
        public long Id { get; set; }

        public long AggregateId { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public int Version { get; set; }

        public DateTime OccurredAt { get; set; }
    }

    
    public class EventSnapshotEntry
    {
        public long Id { get; set; }

        public long AggregateId { get; set; }

        public int Version { get; set; }

        public string SnapshotData { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}