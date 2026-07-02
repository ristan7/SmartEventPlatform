using SmartEventPlatform.EventService.EventSourcing.DomainEvents;

namespace SmartEventPlatform.EventService.EventSourcing
{

    public abstract class AggregateRoot
    {
        private readonly List<EventDomainEvent> _uncommittedEvents = new();

        public long Id { get; protected set; }
        public int Version { get; protected set; }
        public bool IsDeleted { get; protected set; }


        protected void RaiseEvent(EventDomainEvent @event)
        {
            Apply(@event);
            Version++;
            _uncommittedEvents.Add(@event);
        }


        protected abstract void Apply(EventDomainEvent @event);


        public void LoadFromHistory(IEnumerable<EventDomainEvent> history)
        {
            foreach (var @event in history)
            {
                Apply(@event);
                Version++;
            }
        }


        public IReadOnlyList<EventDomainEvent> DequeueUncommittedEvents()
        {
            var events = _uncommittedEvents.ToList();
            _uncommittedEvents.Clear();
            return events;
        }

        public abstract AggregateSnapshot CreateSnapshot();
        public abstract void RestoreSnapshot(AggregateSnapshot snapshot);
    }


    public abstract class AggregateSnapshot
    {
        public long AggregateId { get; set; }
        public int Version { get; set; }
    }

    public class EventAggregate : AggregateRoot
    {
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
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("Event name must not be empty.");
            if (durationInMinutes <= 0)
                throw new ArgumentException("Duration must be a positive number of minutes.");
            if (registrationFee < 0)
                throw new ArgumentException("Registration fee cannot be negative.");
            if (eventDateTime <= DateTime.UtcNow)
                throw new ArgumentException("Event date must be in the future.");

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

        public void Rename(string newName)
        {
            EnsureNotCancelled();
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New name must not be empty.");
            if (newName == EventName)
                throw new InvalidOperationException("New name is the same as the current one.");

            RaiseEvent(new EventRenamedDomainEvent
            {
                OldName = EventName,
                NewName = newName
            });
        }

        public void Reschedule(DateTime newDateTime, int newDurationInMinutes)
        {
            EnsureNotCancelled();
            if (newDateTime <= DateTime.UtcNow)
                throw new ArgumentException("New date must be in the future.");
            if (newDurationInMinutes <= 0)
                throw new ArgumentException("Duration must be a positive number of minutes.");

            RaiseEvent(new EventRescheduledDomainEvent
            {
                OldDateTime = EventDateTime,
                NewDateTime = newDateTime,
                OldDurationInMinutes = DurationInMinutes,
                NewDurationInMinutes = newDurationInMinutes
            });
        }

        public void ChangeFee(decimal newFee)
        {
            EnsureNotCancelled();
            if (newFee < 0)
                throw new ArgumentException("Registration fee cannot be negative.");

            RaiseEvent(new EventFeeChangedDomainEvent
            {
                OldFee = RegistrationFee,
                NewFee = newFee
            });
        }

        public void ChangeLocation(long newLocationId, string newLocationName)
        {
            EnsureNotCancelled();
            if (newLocationId <= 0)
                throw new ArgumentException("Invalid location ID.");
            if (string.IsNullOrWhiteSpace(newLocationName))
                throw new ArgumentException("Location name must not be empty.");

            RaiseEvent(new EventLocationChangedDomainEvent
            {
                OldLocationId = LocationId,
                OldLocationName = LocationName,
                NewLocationId = newLocationId,
                NewLocationName = newLocationName
            });
        }

        public void Cancel(string reason)
        {
            EnsureNotCancelled();
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancellation reason must be provided.");

            RaiseEvent(new EventCancelledDomainEvent
            {
                Reason = reason
            });
        }


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
                throw new InvalidOperationException($"Invalid snapshot type: {snapshot.GetType().Name}");

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
                    throw new InvalidOperationException($"Unknown domain event type: {@event.GetType().Name}");
            }
        }


        private void EnsureNotCancelled()
        {
            if (IsCancelled)
                throw new InvalidOperationException("A cancelled event can no longer be modified.");
        }
    }

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