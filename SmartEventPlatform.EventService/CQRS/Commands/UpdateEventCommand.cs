namespace SmartEventPlatform.EventService.CQRS.Commands
{
    /// <summary>
    /// Komanda za izmjenu postojećeg događaja.
    /// Vraća true ako je event pronađen i ažuriran, false ako nije pronađen.
    /// </summary>
    public class UpdateEventCommand
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }
        public long LocationId { get; set; }
        public long EventTypeId { get; set; }
    }
}