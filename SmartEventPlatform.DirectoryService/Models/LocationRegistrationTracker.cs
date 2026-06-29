namespace SmartEventPlatform.DirectoryService.Models
{
    /// <summary>
    /// Evidencija ukupnog broja potvrđenih registracija po lokaciji.
    ///
    /// Incrementira se u Koraku 3 Sage (record-attendance).
    /// Decrementira se kompenzacijom Koraka 3 (release-attendance).
    ///
    /// Ova tabela daje DirectoryService-u uvid koliko je osoba
    /// ukupno registrovano za događaje na određenoj lokaciji,
    /// bez da mora direktno komunicirati sa RegistrationService-om.
    /// </summary>
    public class LocationRegistrationTracker
    {
        public long LocationId { get; set; }
        public int RegistrationCount { get; set; }
    }
}