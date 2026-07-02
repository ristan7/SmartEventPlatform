namespace SmartEventPlatform.EventService.EventSourcing
{
    public record CreateEventSourcedRequest(
        long EventId,
        string EventName,
        string Agenda,
        DateTime EventDateTime,
        int DurationInMinutes,
        decimal RegistrationFee,
        long LocationId,
        string LocationName,
        long EventTypeId);

    public record RenameEventRequest(string NewName);

    public record RescheduleEventRequest(DateTime NewDateTime, int NewDurationInMinutes);

    public record ChangeFeeRequest(decimal NewFee);

    public record ChangeLocationRequest(long NewLocationId, string NewLocationName);

    public record CancelEventRequest(string Reason);

    public class EventAggregateResponse
    {
        public long EventId { get; set; }
        public int Version { get; set; }
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

    public class CreateSnapshotResponse
    {
        public string Message { get; set; } = string.Empty;
        public int Version { get; set; }
    }
}