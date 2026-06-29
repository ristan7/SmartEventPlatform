namespace SmartEventPlatform.RegistrationService.Models
{
    /// <summary>
    /// Praćenje stanja Saga procesa za registraciju učesnika.
    /// Svaki red u ovoj tabeli predstavlja jednu Saga instancu.
    /// Status se mijenja kako Saga prolazi kroz korake,
    /// a u slučaju greške prelazi u kompenzacione korake.
    /// </summary>
    public class SagaState
    {
        public long SagaId { get; set; }

        /// <summary>
        /// Mogući statusi:
        ///   Started            - Saga je pokrenuta
        ///   RegistrationCreated - Korak 1 završen: PENDING registracija kreirana
        ///   SpotReserved       - Korak 2 završen: Mjesto rezervisano u EventService
        ///   AttendanceRecorded - Korak 3 završen: Prisustvo zabilježeno u DirectoryService
        ///   Completed          - Korak 4 završen: Registracija CONFIRMED, email poslan
        ///   Compensating       - Detektovana greška, kompenzacije u toku
        ///   Compensated        - Sve kompenzacije uspješno izvršene
        ///   Failed             - Greška, kompenzacija nije uspjela
        /// </summary>
        public string Status { get; set; } = "Started";

        // Podaci koji se čuvaju da bi kompenzacija znala šta da poništi
        public long? RegistrationId { get; set; }
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public long LocationId { get; set; }

        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}