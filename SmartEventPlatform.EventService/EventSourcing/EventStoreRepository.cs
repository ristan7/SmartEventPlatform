using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.EventSourcing.DomainEvents;

namespace SmartEventPlatform.EventService.EventSourcing
{
    /// <summary>
    /// Repozitorij koji persostira i učitava EventAggregate koristeći Event Sourcing.
    ///
    /// Ekvivalent InMemoryDatabase iz primjera, ali umjesto Dictionary-ja koristi SQL Server bazu.
    ///
    /// Principi:
    ///  • Save() — uzima neobjavljene događaje agregata, serializuje ih u JSON i čuva u EventStoreEntries tabeli
    ///  • Load() — čita sve sačuvane događaje za agregat, primjenjuje ih redom i rekonstruiše stanje
    ///  • Load() sa snapshotom — ako postoji snapshot, restauriše ga pa primjenjuje samo novije događaje
    ///  • CreateSnapshot() — serializuje trenutno stanje agregata i čuva u EventSnapshotEntries tabeli
    /// </summary>
    public class EventStoreRepository
    {
        private readonly EventDbContext _db;
        private readonly ILogger<EventStoreRepository> _logger;

        
        private static readonly Dictionary<string, Type> _knownEventTypes = new()
        {
            [nameof(EventCreatedDomainEvent)] = typeof(EventCreatedDomainEvent),
            [nameof(EventRenamedDomainEvent)] = typeof(EventRenamedDomainEvent),
            [nameof(EventRescheduledDomainEvent)] = typeof(EventRescheduledDomainEvent),
            [nameof(EventFeeChangedDomainEvent)] = typeof(EventFeeChangedDomainEvent),
            [nameof(EventLocationChangedDomainEvent)] = typeof(EventLocationChangedDomainEvent),
            [nameof(EventCancelledDomainEvent)] = typeof(EventCancelledDomainEvent),
        };

        public EventStoreRepository(EventDbContext db, ILogger<EventStoreRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        //  SAVE — persistiraj neobjavljene događaje agregata
        //  Identično InMemoryDatabase.Save<T>() iz primjera
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Preuzima neobjavljene domenskie događaje agregata, serializuje ih u JSON
        /// i upisuje u bazu hronološkim redom. Svaki događaj dobija sledeću verziju.
        /// </summary>
        public async Task SaveAsync(EventAggregate aggregate, CancellationToken ct = default)
        {
            var uncommittedEvents = aggregate.DequeueUncommittedEvents();

            if (!uncommittedEvents.Any())
            {
                _logger.LogDebug("EventSourcing: Nema novih događaja za agregat {AggregateId}", aggregate.Id);
                return;
            }

            // Pronađi najnoviju verziju u bazi da bismo dodijelili sledeće verzije
            var lastVersion = await _db.EventStoreEntries
                .Where(e => e.AggregateId == aggregate.Id)
                .MaxAsync(e => (int?)e.Version, ct) ?? 0;

            foreach (var domainEvent in uncommittedEvents)
            {
                lastVersion++;
                var entry = new EventStoreEntry
                {
                    AggregateId = aggregate.Id,
                    EventType = domainEvent.GetType().Name,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    Version = lastVersion,
                    OccurredAt = domainEvent.OccurredAt
                };

                _db.EventStoreEntries.Add(entry);
                _logger.LogInformation(
                    "EventSourcing: Čuvam događaj {EventType} v{Version} za agregat {AggregateId}",
                    entry.EventType, entry.Version, entry.AggregateId);
            }

            await _db.SaveChangesAsync(ct);
        }

        // ─────────────────────────────────────────────────────────────────
        //  LOAD — rekonstruiši agregat iz historije događaja
        //  Identično InMemoryDatabase.Load<T>() iz primjera
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Učitava EventAggregate rekonstrukcijom stanja iz historije sačuvanih ereignaje.
        ///
        /// Algoritam (identičan primjeru):
        ///   1. Provjeri postoji li snapshot → ako da, restauriraj ga (Version = snapshot.Version)
        ///   2. Uzmi sve događaje NAKON verzije snapshota
        ///   3. Primjeni ih redom koristeći LoadFromHistory()
        ///
        /// Vraća null ako agregat ne postoji.
        /// </summary>
        public async Task<EventAggregate?> LoadAsync(long aggregateId, CancellationToken ct = default)
        {
            // Provjeri postoji li uopšte ovaj agregat
            var exists = await _db.EventStoreEntries
                .AnyAsync(e => e.AggregateId == aggregateId, ct);

            if (!exists)
            {
                _logger.LogDebug("EventSourcing: Agregat {AggregateId} ne postoji", aggregateId);
                return null;
            }

            var aggregate = new EventAggregate();
            var startFromVersion = 0;

            // Pokušaj restauraciju snapshota
            var snapshotEntry = await _db.EventSnapshotEntries
                .Where(s => s.AggregateId == aggregateId)
                .OrderByDescending(s => s.Version)
                .FirstOrDefaultAsync(ct);

            if (snapshotEntry != null)
            {
                var snapshot = JsonSerializer.Deserialize<EventSnapshot>(snapshotEntry.SnapshotData);
                if (snapshot != null)
                {
                    aggregate.RestoreSnapshot(snapshot);
                    startFromVersion = snapshotEntry.Version;
                    _logger.LogDebug(
                        "EventSourcing: Restaurisan snapshot v{Version} za agregat {AggregateId}",
                        startFromVersion, aggregateId);
                }
            }

            // Učitaj događaje nastale NAKON snapshota
            var eventEntries = await _db.EventStoreEntries
                .Where(e => e.AggregateId == aggregateId && e.Version > startFromVersion)
                .OrderBy(e => e.Version)
                .ToListAsync(ct);

            var domainEvents = eventEntries
                .Select(Deserialize)
                .Where(e => e != null)
                .Cast<EventDomainEvent>()
                .ToList();

            if (domainEvents.Any())
            {
                aggregate.LoadFromHistory(domainEvents);
                _logger.LogDebug(
                    "EventSourcing: Primijenjeno {Count} događaja za agregat {AggregateId} (početna verzija: {StartVersion})",
                    domainEvents.Count, aggregateId, startFromVersion);
            }

            return aggregate;
        }

        // ─────────────────────────────────────────────────────────────────
        //  SNAPSHOT — sačuvaj trenutno stanje agregata
        //  Identično InMemoryDatabase.CreateSnapshot<T>() iz primjera
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Kreira i persistira snapshot trenutnog stanja agregata.
        /// Zamjenjuje prethodni snapshot (čuva samo najnoviji po agregatu).
        /// </summary>
        public async Task CreateSnapshotAsync(EventAggregate aggregate, CancellationToken ct = default)
        {
            var snapshot = (EventSnapshot)aggregate.CreateSnapshot();

            // Ukloni stari snapshot ako postoji
            var existing = await _db.EventSnapshotEntries
                .Where(s => s.AggregateId == aggregate.Id)
                .ToListAsync(ct);
            _db.EventSnapshotEntries.RemoveRange(existing);

            _db.EventSnapshotEntries.Add(new EventSnapshotEntry
            {
                AggregateId = aggregate.Id,
                Version = aggregate.Version,
                SnapshotData = JsonSerializer.Serialize(snapshot),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "EventSourcing: Kreiran snapshot v{Version} za agregat {AggregateId}",
                aggregate.Version, aggregate.Id);
        }

        // ─────────────────────────────────────────────────────────────────
        //  HISTORIJA — vrati listu svih sačuvanih domenskih događaja
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Vraća kompletnu listu svih sačuvanih domenskih događaja za dati agregat,
        /// zajedno sa njihovim metapodacima (tip, verzija, vrijeme nastanka).
        /// Namijenjeno za prikaz historije promjena.
        /// </summary>
        public async Task<List<EventHistoryItem>> GetHistoryAsync(long aggregateId, CancellationToken ct = default)
        {
            var entries = await _db.EventStoreEntries
                .Where(e => e.AggregateId == aggregateId)
                .OrderBy(e => e.Version)
                .ToListAsync(ct);

            return entries.Select(e => new EventHistoryItem
            {
                Version = e.Version,
                EventType = e.EventType,
                OccurredAt = e.OccurredAt,
                Payload = e.Payload
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────
        //  HELPER — deserijalizuj domenski događaj iz JSON-a
        // ─────────────────────────────────────────────────────────────────

        private EventDomainEvent? Deserialize(EventStoreEntry entry)
        {
            if (!_knownEventTypes.TryGetValue(entry.EventType, out var type))
            {
                _logger.LogWarning("EventSourcing: Nepoznat tip događaja '{EventType}' — preskačem", entry.EventType);
                return null;
            }

            return (EventDomainEvent?)JsonSerializer.Deserialize(entry.Payload, type);
        }
    }

    /// <summary>DTO za prikaz jednog historijskog unosa.</summary>
    public class EventHistoryItem
    {
        public int Version { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}