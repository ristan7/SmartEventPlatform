using SmartEventPlatform.EventService.EventSourcing.DomainEvents;

namespace SmartEventPlatform.EventService.EventSourcing
{
    /// <summary>
    /// Bazna klasa za sve agregate koji koriste Event Sourcing.
    /// Identična ulozi AggregateRoot iz primjera — drži listu nepersistiranih događaja
    /// i rekonstruiše stanje primjenom historije događaja.
    /// </summary>
    public abstract class AggregateRoot
    {
        private readonly List<EventDomainEvent> _uncommittedEvents = new();

        public long Id { get; protected set; }
        public int Version { get; protected set; }
        public bool IsDeleted { get; protected set; }

        /// <summary>
        /// Primjeni novi domenski događaj na agregat:
        /// 1. Pozovi Apply() da promijeniš stanje
        /// 2. Uvećaj Version
        /// 3. Dodaj u listu nepersistiranih događaja
        /// </summary>
        protected void RaiseEvent(EventDomainEvent @event)
        {
            Apply(@event);
            Version++;
            _uncommittedEvents.Add(@event);
        }

        /// <summary>
        /// Svaki konkretan agregat definišeKako primjenjuje svaki tip događaja.
        /// Ovo je jedino mjesto gdje se stanje direktno mijenja.
        /// </summary>
        protected abstract void Apply(EventDomainEvent @event);

        /// <summary>
        /// Rekonstruiše stanje agregata od historije sačuvanih događaja.
        /// Identično LoadFromHistory() iz primjera.
        /// </summary>
        public void LoadFromHistory(IEnumerable<EventDomainEvent> history)
        {
            foreach (var @event in history)
            {
                Apply(@event);
                Version++;
            }
        }

        /// <summary>Vraća i briše listu nepersistiranih događaja. Poziva se pri snimanju.</summary>
        public IReadOnlyList<EventDomainEvent> DequeueUncommittedEvents()
        {
            var events = _uncommittedEvents.ToList();
            _uncommittedEvents.Clear();
            return events;
        }

        public abstract AggregateSnapshot CreateSnapshot();
        public abstract void RestoreSnapshot(AggregateSnapshot snapshot);
    }

    /// <summary>
    /// Bazna klasa za snapshot — identična AggregateSnapshot iz primjera.
    /// Čuva stanje agregata u određenom trenutku radi efikasnog učitavanja.
    /// </summary>
    public abstract class AggregateSnapshot
    {
        public long AggregateId { get; set; }
        public int Version { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  KONKRETNI AGREGAT: EventAggregate
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Event-Sourced reprezentacija stručnog događaja.
    /// Identičan principu BankAccount iz primjera:
    ///   • Stanje se nikada ne mijenja direktno (nema public settera koji se poziva izvana)
    ///   • Svaka promjena ide kroz metodu koja kreira domenski događaj i poziva RaiseEvent()
    ///   • Apply() je jedino mjesto koje direktno mijenja property-je
    /// </summary>
    public class EventAggregate : AggregateRoot
    {
        // ── Stanje agregata (ne mijenjati direktno izvan Apply!) ──────────
        public string EventName { get; private set; } = string.Empty;
        public string Agenda { get; private set; } = string.Empty;
        public DateTime EventDateTime { get; private set; }
        public int DurationInMinutes { get; private set; }
        public decimal RegistrationFee { get; private set; }
        public long LocationId { get; private set; }
        public string LocationName { get; private set; } = string.Empty;
        public long EventTypeId { get; private set; }
        public bool IsCancelled { get; private set; }
        public string? CancellationReason { get; private set; }

        // ─────────────────────────────────────────────────────────────────
        //  FACTORY METODA — jedini način kreiranja novog agregata
        //  (Identično BankAccount.Create() iz primjera)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Kreira novi stručni događaj. Validira poslovna pravila PRIJE generisanja domenskog događaja.
        /// Nikada ne postavljamo stanje direktno — idemo kroz RaiseEvent.
        /// </summary>
        public static EventAggregate Create(
            long eventId,
            string eventName,
            string agenda,
            DateTime eventDateTime,
            int durationInMinutes,
            decimal registrationFee,
            long locationId,
            string locationName,
            long eventTypeId)
        {
            // ── VALIDACIJA POSLOVNIH PRAVILA ──────────────────────────────
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("Naziv događaja ne smije biti prazan.");
            if (durationInMinutes <= 0)
                throw new ArgumentException("Trajanje mora biti pozitivan broj minuta.");
            if (registrationFee < 0)
                throw new ArgumentException("Cijena kotizacije ne može biti negativna.");
            if (eventDateTime <= DateTime.UtcNow)
                throw new ArgumentException("Datum događaja mora biti u budućnosti.");

            var aggregate = new EventAggregate();
            aggregate.RaiseEvent(new EventCreatedDomainEvent
            {
                EventId = eventId,
                EventName = eventName,
                Agenda = agenda,
                EventDateTime = eventDateTime,
                DurationInMinutes = durationInMinutes,
                RegistrationFee = registrationFee,
                LocationId = locationId,
                LocationName = locationName,
                EventTypeId = eventTypeId
            });

            return aggregate;
        }

        // ─────────────────────────────────────────────────────────────────
        //  POSLOVNE OPERACIJE — svaka validira pa kreira domenski događaj
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Promijeni naziv stručnog događaja.</summary>
        public void Rename(string newName)
        {
            EnsureNotCancelled();
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Novi naziv ne smije biti prazan.");
            if (newName == EventName)
                throw new InvalidOperationException("Novi naziv je isti kao trenutni.");

            RaiseEvent(new EventRenamedDomainEvent
            {
                OldName = EventName,
                NewName = newName
            });
        }

        /// <summary>Promijeni datum, vrijeme i trajanje stručnog događaja.</summary>
        public void Reschedule(DateTime newDateTime, int newDurationInMinutes)
        {
            EnsureNotCancelled();
            if (newDateTime <= DateTime.UtcNow)
                throw new ArgumentException("Novi datum mora biti u budućnosti.");
            if (newDurationInMinutes <= 0)
                throw new ArgumentException("Trajanje mora biti pozitivan broj minuta.");

            RaiseEvent(new EventRescheduledDomainEvent
            {
                OldDateTime = EventDateTime,
                NewDateTime = newDateTime,
                OldDurationInMinutes = DurationInMinutes,
                NewDurationInMinutes = newDurationInMinutes
            });
        }

        /// <summary>Promijeni cijenu kotizacije.</summary>
        public void ChangeFee(decimal newFee)
        {
            EnsureNotCancelled();
            if (newFee < 0)
                throw new ArgumentException("Cijena kotizacije ne može biti negativna.");

            RaiseEvent(new EventFeeChangedDomainEvent
            {
                OldFee = RegistrationFee,
                NewFee = newFee
            });
        }

        /// <summary>Promijeni lokaciju stručnog događaja.</summary>
        public void ChangeLocation(long newLocationId, string newLocationName)
        {
            EnsureNotCancelled();
            if (newLocationId <= 0)
                throw new ArgumentException("Neispravni ID lokacije.");
            if (string.IsNullOrWhiteSpace(newLocationName))
                throw new ArgumentException("Naziv lokacije ne smije biti prazan.");

            RaiseEvent(new EventLocationChangedDomainEvent
            {
                OldLocationId = LocationId,
                OldLocationName = LocationName,
                NewLocationId = newLocationId,
                NewLocationName = newLocationName
            });
        }

        /// <summary>Otkaži stručni događaj. Otkazan događaj se ne može više mijenjati.</summary>
        public void Cancel(string reason)
        {
            EnsureNotCancelled();
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Razlog otkazivanja mora biti naveden.");

            RaiseEvent(new EventCancelledDomainEvent
            {
                Reason = reason
            });
        }

        // ─────────────────────────────────────────────────────────────────
        //  SNAPSHOT
        // ─────────────────────────────────────────────────────────────────

        public override AggregateSnapshot CreateSnapshot()
        {
            return new EventSnapshot
            {
                AggregateId = Id,
                Version = Version,
                EventName = EventName,
                Agenda = Agenda,
                EventDateTime = EventDateTime,
                DurationInMinutes = DurationInMinutes,
                RegistrationFee = RegistrationFee,
                LocationId = LocationId,
                LocationName = LocationName,
                EventTypeId = EventTypeId,
                IsCancelled = IsCancelled,
                CancellationReason = CancellationReason
            };
        }

        public override void RestoreSnapshot(AggregateSnapshot snapshot)
        {
            if (snapshot is not EventSnapshot s)
                throw new InvalidOperationException($"Neispravan tip snapshot-a: {snapshot.GetType().Name}");

            Id = s.AggregateId;
            Version = s.Version;
            EventName = s.EventName;
            Agenda = s.Agenda;
            EventDateTime = s.EventDateTime;
            DurationInMinutes = s.DurationInMinutes;
            RegistrationFee = s.RegistrationFee;
            LocationId = s.LocationId;
            LocationName = s.LocationName;
            EventTypeId = s.EventTypeId;
            IsCancelled = s.IsCancelled;
            CancellationReason = s.CancellationReason;
        }

        // ─────────────────────────────────────────────────────────────────
        //  APPLY — jedino mjesto gdje se stanje direktno mijenja
        //  (Identičan switch iz BankAccount.Apply() u primjeru)
        // ─────────────────────────────────────────────────────────────────

        protected override void Apply(EventDomainEvent @event)
        {
            switch (@event)
            {
                case EventCreatedDomainEvent e:
                    Id = e.EventId;
                    EventName = e.EventName;
                    Agenda = e.Agenda;
                    EventDateTime = e.EventDateTime;
                    DurationInMinutes = e.DurationInMinutes;
                    RegistrationFee = e.RegistrationFee;
                    LocationId = e.LocationId;
                    LocationName = e.LocationName;
                    EventTypeId = e.EventTypeId;
                    IsCancelled = false;
                    break;

                case EventRenamedDomainEvent e:
                    EventName = e.NewName;
                    break;

                case EventRescheduledDomainEvent e:
                    EventDateTime = e.NewDateTime;
                    DurationInMinutes = e.NewDurationInMinutes;
                    break;

                case EventFeeChangedDomainEvent e:
                    RegistrationFee = e.NewFee;
                    break;

                case EventLocationChangedDomainEvent e:
                    LocationId = e.NewLocationId;
                    LocationName = e.NewLocationName;
                    break;

                case EventCancelledDomainEvent e:
                    IsCancelled = true;
                    CancellationReason = e.Reason;
                    break;

                default:
                    throw new InvalidOperationException($"Nepoznat tip domenskog događaja: {@event.GetType().Name}");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  HELPER
        // ─────────────────────────────────────────────────────────────────

        private void EnsureNotCancelled()
        {
            if (IsCancelled)
                throw new InvalidOperationException("Otkazan događaj se više ne može mijenjati.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SNAPSHOT — identičan BankAccountSnapshot iz primjera
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot stanja EventAggregate u određenom trenutku.
    /// Koristi se za efikasno učitavanje bez primjene svih historijskih događaja.
    /// </summary>
    public class EventSnapshot : AggregateSnapshot
    {
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }
        public long LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public long EventTypeId { get; set; }
        public bool IsCancelled { get; set; }
        public string? CancellationReason { get; set; }
    }
}