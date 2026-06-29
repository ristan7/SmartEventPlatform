namespace SmartEventPlatform.EventService.EventSourcing.DomainEvents
{
    /// <summary>
    /// Bazna klasa za sve domain događaje vezane za Event agregat.
    /// Svaki domain događaj bilježi šta se desilo i kada — nikada se ne mijenja nakon što je sačuvan.
    /// </summary>
    public abstract class EventDomainEvent
    {
        protected EventDomainEvent()
        {
            Id = Guid.NewGuid();
            OccurredAt = DateTime.UtcNow;
        }

        /// <summary>Jedinstven identifikator ovog domenskog događaja.</summary>
        public Guid Id { get; }

        /// <summary>Kada se dogodio ovaj domenski događaj (UTC).</summary>
        public DateTime OccurredAt { get; }
    }
}