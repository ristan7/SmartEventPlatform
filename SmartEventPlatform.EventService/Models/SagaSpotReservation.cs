namespace SmartEventPlatform.EventService.Models
{
    
    public class SagaSpotReservation
    {
        public long Id { get; set; }
        public long SagaId { get; set; }
        public long EventId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}