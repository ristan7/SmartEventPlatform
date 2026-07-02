namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    
    public abstract class EventDomainEvent
    {
        protected EventDomainEvent()
        {
            Id = Guid.NewGuid();
            OccurredAt = DateTime.UtcNow;
        }
        
        public Guid Id { get; }

        public DateTime OccurredAt { get; }
    }
}