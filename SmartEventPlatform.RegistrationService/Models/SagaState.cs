namespace SmartEventPlatform.RegistrationService.Models
{
    
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

        public long? RegistrationId { get; set; }
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public long LocationId { get; set; }

        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}