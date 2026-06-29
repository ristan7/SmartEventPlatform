namespace SmartEventPlatform.EventService.CQRS.Commands
{
    /// <summary>
    /// Komanda za kreiranje novog događaja.
    /// Obična C# klasa — bez gotovih biblioteka.
    /// Command operacije vraćaju samo rezultat izvršavanja (ID), nikad puni domain objekat.
    /// </summary>
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