namespace SmartEventPlatform.EventService.CQRS.ReadModels
{
    /// <summary>
    /// Read-only model koji se koristi isključivo za upite (Query strana CQRS-a).
    /// Ovaj model NIKADA ne smije biti korišten za izmjenu stanja sistema.
    /// </summary>
    public class EventReadModel
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }

        public long LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        public int Capacity { get; set; }

        public long EventTypeId { get; set; }
        public string EventTypeName { get; set; } = string.Empty;

        public List<string> Speakers { get; set; } = new();
    }
}