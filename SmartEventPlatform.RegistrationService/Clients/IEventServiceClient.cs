using SmartEventPlatform.Contracts.Events;

namespace SmartEventPlatform.RegistrationService.Clients;

public interface IEventServiceClient
{
    Task<List<EventDto>> GetAllEventsAsync();
    Task<EventRegistrationInfoDto> GetRegistrationInfoAsync(long eventId);

    // ── Saga metode ──────────────────────────────────────────────────────────
    // Korak 2: Rezerviši mjesto u EventService (privremena rezervacija)
    // Vraća true ako je rezervacija uspješna, false ako nema kapaciteta
    Task<bool> ReserveSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken);

    // Korak 2 kompenzacija: Otkaži rezervaciju (poništava ReserveSpotAsync)
    Task ReleaseSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken);

    // Korak 4: Potvrdi rezervaciju - prebaci iz privremene u stvarnu evidenciju
    Task ConfirmSpotAsync(long eventId, long sagaId, CancellationToken cancellationToken);
}