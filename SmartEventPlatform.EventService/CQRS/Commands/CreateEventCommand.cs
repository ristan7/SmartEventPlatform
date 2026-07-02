namespace SmartEventPlatform.EventService.CQRS.Commands
{
    
    public class CreateEventCommand
    {
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }
        public long LocationId { get; set; }
        public long EventTypeId { get; set; }
    }
}