namespace SmartEventPlatform.EventService.Models
{
    /// <summary>
    /// Privremena rezervacija mjesta za Saga proces.
    ///
    /// Kada RegistrationService pokrene Sagu i dostigne Korak 2,
    /// EventService upisuje red u ovu tabelu kao "privremenu rezervaciju".
    /// Ovo sprečava da paralelne Sage prekorače kapacitet dok su u toku.
    ///
    /// Kada Saga uspješno završi (Korak 4), rezervacija se prebacuje
    /// u EventRegistrationTracker (confirm-spot endpoint).
    ///
    /// Kada Saga pukne i ide u kompenzaciju, rezervacija se briše
    /// (release-spot endpoint).
    /// </summary>
    public class SagaSpotReservation
    {
        public long Id { get; set; }
        public long SagaId { get; set; }
        public long EventId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}