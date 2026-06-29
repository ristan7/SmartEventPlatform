namespace SmartEventPlatform.EventService.EventSourcing
{
    /// <summary>
    /// EF Core entitet koji predstavlja jedan sačuvan domenski događaj u bazi.
    /// Ekvivalent entry-ja u InMemoryDatabase._events iz primjera, ali perzistentan.
    /// Payload je JSON-serializovan domenski događaj.
    /// </summary>
    public class EventStoreEntry
    {
        public long Id { get; set; }

        /// <summary>ID agregata (EventAggregate) na koji se ovaj događaj odnosi.</summary>
        public long AggregateId { get; set; }

        /// <summary>Puni tip domenskog događaja (npr. "EventCreatedDomainEvent").</summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>JSON reprezentacija domenskog događaja.</summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>Verzija agregata NAKON primjene ovog događaja. Omogućava optimistic concurrency.</summary>
        public int Version { get; set; }

        /// <summary>Kada je događaj nastao.</summary>
        public DateTime OccurredAt { get; set; }
    }

    /// <summary>
    /// EF Core entitet koji čuva snapshot stanja agregata.
    /// Ekvivalent entry-ja u InMemoryDatabase._snapshots iz primjera.
    /// Omogućava rekonstrukciju od snapshota + kasniji događaji (bez prolaska kroz sve).
    /// </summary>
    public class EventSnapshotEntry
    {
        public long Id { get; set; }

        /// <summary>ID agregata čiji snapshot je sačuvan.</summary>
        public long AggregateId { get; set; }

        /// <summary>Verzija agregata u momentu kreiranja snapshota.</summary>
        public int Version { get; set; }

        /// <summary>JSON reprezentacija snapshot objekta.</summary>
        public string SnapshotData { get; set; } = string.Empty;

        /// <summary>Kada je snapshot kreiran.</summary>
        public DateTime CreatedAt { get; set; }
    }
}