namespace SmartEventPlatform.RegistrationService.Models
{
    /// <summary>
    /// Pracenje stanja jedne Saga Koreografija instance.
    ///
    /// Koreografija nema centralnog orkestratora koji biljezi stanje,
    /// ali zadatak zahtijeva evidenciju toka procesa — zato RegistrationService
    /// (inicijator Sage) vodi ovu tabelu. Svaki dolazni dogadjaj azurira status.
    ///
    /// Tok statusa:
    ///   Started            → Saga pokrenuta, PENDING registracija kreirana
    ///   SpotReserved       → EventService rezervisao mjesto (Korak 2)
    ///   AttendanceRecorded → DirectoryService zabiljezio prisustvo (Korak 3)
    ///   Completed          → Registracija potvrdjena, email poslan (Korak 4)
    ///   Compensated        → Kompenzacija uspjesna (saga ponistena bez greske)
    ///   Failed             → Neocekivana greska, potrebna rucna intervencija
    /// </summary>
    public class SagaChoreographyState
    {
        public long SagaId { get; set; }

        /// <summary>Jedinstven ID koji se prenosi kroz sve dogadjaje Sage.</summary>
        public Guid CorrelationId { get; set; }

        public string Status { get; set; } = "Started";

        public long? RegistrationId { get; set; }
        public long EventId { get; set; }
        public long ParticipantId { get; set; }
        public long LocationId { get; set; }

        // Cuvamo detalje za email jer consumer nema HTTP pristup
        public string ParticipantFirstName { get; set; } = string.Empty;
        public string ParticipantLastName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }

        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}