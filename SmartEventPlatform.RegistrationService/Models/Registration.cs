namespace SmartEventPlatform.RegistrationService.Models
{
    public class Registration
    {
        public long RegistrationId { get; set; }
        public DateTime RegistrationDate { get; set; }

        public long EventId { get; set; }

        public long ParticipantId { get; set; }
        public Participant? Participant { get; set; }

        /// <summary>
        /// Status registracije u kontekstu Saga procesa.
        ///   Pending   - Registracija je kreirana ali Saga još nije završena
        ///   Confirmed - Saga je uspješno završena, registracija je aktivna
        ///   Cancelled - Saga je kompenzovana, registracija je poništena
        /// Podrazumijevano: "Confirmed" za stare (pre-Saga) registracije radi kompatibilnosti.
        /// </summary>
        public string Status { get; set; } = "Confirmed";
    }
}