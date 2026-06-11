namespace SmartEventPlatform.Contracts.Events
{
    public class AvailableEventDto
    {
        public long EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal RegistrationFee { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int RegisteredCount { get; set; }
        public int AvailableSeats => Capacity - RegisteredCount;
        public List<string> Speakers { get; set; } = new();
    }
}
