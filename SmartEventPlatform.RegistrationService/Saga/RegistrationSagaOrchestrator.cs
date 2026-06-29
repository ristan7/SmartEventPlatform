using SmartEventPlatform.Contracts.Events;
using SmartEventPlatform.RegistrationService.Clients;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Messaging;
using SmartEventPlatform.RegistrationService.Models;

namespace SmartEventPlatform.RegistrationService.Saga;

/// <summary>
/// Saga Orkestrator za registraciju učesnika na događaj.
///
/// Orkestrator je centralna komponenta koja koordinira slijedеće korake:
///
///   [Korak 1] RegistrationService: Kreiranje PENDING registracije
///   [Korak 2] EventService:        Rezervacija mjesta (provjera kapaciteta)
///   [Korak 3] DirectoryService:    Evidencija prisustva na lokaciji
///   [Korak 4] RegistrationService: Potvrda registracije (CONFIRMED) + email + finalizacija u EventService
///
/// Ako bilo koji korak ne uspije, pokreću se kompenzacione akcije
/// obrnutim redoslijedom (Korak N-1 → Korak 1).
///
/// Stanje Sage se čuva u bazi (SagaStates tabela) kako bi se
/// moglo pratiti i debuggirati svaki Saga proces.
/// </summary>
public class RegistrationSagaOrchestrator
{
    private readonly RegistrationDbContext _context;
    private readonly IEventServiceClient _eventServiceClient;
    private readonly IDirectoryServiceClient _directoryServiceClient;
    private readonly IEmailQueuePublisher _emailQueuePublisher;
    private readonly ILogger<RegistrationSagaOrchestrator> _logger;

    public RegistrationSagaOrchestrator(
        RegistrationDbContext context,
        IEventServiceClient eventServiceClient,
        IDirectoryServiceClient directoryServiceClient,
        IEmailQueuePublisher emailQueuePublisher,
        ILogger<RegistrationSagaOrchestrator> logger)
    {
        _context = context;
        _eventServiceClient = eventServiceClient;
        _directoryServiceClient = directoryServiceClient;
        _emailQueuePublisher = emailQueuePublisher;
        _logger = logger;
    }

    /// <summary>
    /// Rezultat izvršavanja Sage.
    /// </summary>
    public record SagaResult(
        bool Success,
        long? RegistrationId,
        string? ErrorMessage);

    /// <summary>
    /// Ulazna tačka za pokretanje Saga procesa.
    /// Poziva je RegistrationsController.Create umjesto direktnog kreiranja registracije.
    /// </summary>
    public async Task<SagaResult> ExecuteAsync(
        long eventId,
        long participantId,
        DateTime registrationDate,
        long locationId,      // potreban za Korak 3
        string eventName,     // potreban za email
        string participantFirstName,
        string participantLastName,
        string participantEmail,
        CancellationToken cancellationToken)
    {
        // ── Inicijalizacija Saga stanja ──────────────────────────────────────
        var saga = new SagaState
        {
            Status = "Started",
            EventId = eventId,
            ParticipantId = participantId,
            LocationId = locationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SagaStates.Add(saga);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Saga {SagaId}] Pokrenuta. EventId={EventId}, ParticipantId={ParticipantId}.",
            saga.SagaId, eventId, participantId);

        // ════════════════════════════════════════════════════════════════════
        // KORAK 1: Kreiranje PENDING registracije
        // ════════════════════════════════════════════════════════════════════
        Registration registration;
        await using (var tx = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                _logger.LogInformation("[Saga {SagaId}] Korak 1: Kreiram PENDING registraciju...", saga.SagaId);

                registration = new Registration
                {
                    EventId = eventId,
                    ParticipantId = participantId,
                    RegistrationDate = registrationDate,
                    Status = "Pending"          // označena kao Pending dok Saga ne završi
                };
                _context.Registrations.Add(registration);

                saga.Status = "RegistrationCreated";
                saga.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);

                saga.RegistrationId = registration.RegistrationId;
                saga.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "[Saga {SagaId}] Korak 1 završen: RegistrationId={RegistrationId} (PENDING).",
                    saga.SagaId, registration.RegistrationId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "[Saga {SagaId}] Korak 1 PUKAO. Saga završena bez kompenzacija.", saga.SagaId);
                saga.Status = "Failed";
                saga.FailureReason = $"Korak 1 pukao: {ex.Message}";
                saga.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new SagaResult(false, null, "Greška pri kreiranju registracije.");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // KORAK 2: Rezervacija mjesta u EventService
        // ════════════════════════════════════════════════════════════════════
        bool spotReserved;
        try
        {
            _logger.LogInformation(
                "[Saga {SagaId}] Korak 2: Rezervišem mjesto u EventService (EventId={EventId})...",
                saga.SagaId, eventId);

            spotReserved = await _eventServiceClient.ReserveSpotAsync(eventId, saga.SagaId, cancellationToken);

            if (!spotReserved)
            {
                // Kapacitet popunjen - ovo nije tehnička greška, nego poslovna
                _logger.LogWarning(
                    "[Saga {SagaId}] Korak 2: EventService odbio rezervaciju (kapacitet popunjen). " +
                    "Pokrećem kompenzaciju od Koraka 1.",
                    saga.SagaId);

                await CompensateStep1Async(saga, "Kapacitet događaja je popunjen.", cancellationToken);
                return new SagaResult(false, null, "Registracija nije moguća jer je kapacitet lokacije dostignut.");
            }

            saga.Status = "SpotReserved";
            saga.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[Saga {SagaId}] Korak 2 završen: Mjesto rezervisano.", saga.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Saga {SagaId}] Korak 2 PUKAO. Pokrećem kompenzaciju Koraka 1.", saga.SagaId);
            await CompensateStep1Async(saga, $"Korak 2 pukao: {ex.Message}", cancellationToken);
            return new SagaResult(false, null, "Greška pri rezervaciji mjesta. Registracija je otkazana.");
        }

        // ════════════════════════════════════════════════════════════════════
        // KORAK 3: Evidencija prisustva u DirectoryService
        // ════════════════════════════════════════════════════════════════════
        try
        {
            _logger.LogInformation(
                "[Saga {SagaId}] Korak 3: Bilježim prisustvo u DirectoryService (LocationId={LocationId})...",
                saga.SagaId, locationId);

            await _directoryServiceClient.RecordAttendanceAsync(locationId, saga.SagaId, cancellationToken);

            saga.Status = "AttendanceRecorded";
            saga.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[Saga {SagaId}] Korak 3 završen: Prisustvo evidentirano.", saga.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Saga {SagaId}] Korak 3 PUKAO. Pokrećem kompenzacije Koraka 2 i Koraka 1.",
                saga.SagaId);

            saga.Status = "Compensating";
            saga.FailureReason = $"Korak 3 pukao: {ex.Message}";
            saga.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Kompenzacija ide OBRNUTIM REDOSLIJEDOM: Korak 2 pa Korak 1
            await CompensateStep2Async(saga, cancellationToken);
            await CompensateStep1Async(saga, $"Korak 3 pukao: {ex.Message}", cancellationToken);
            return new SagaResult(false, null, "Greška pri evidenciji prisustva. Registracija je otkazana.");
        }

        // ════════════════════════════════════════════════════════════════════
        // KORAK 4: Potvrda registracije (CONFIRMED) + email + finalizacija
        // ════════════════════════════════════════════════════════════════════
        try
        {
            _logger.LogInformation(
                "[Saga {SagaId}] Korak 4: Potvrđujem registraciju (RegistrationId={RegId})...",
                saga.SagaId, registration.RegistrationId);

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            // 4a. Postavi status registracije na CONFIRMED
            registration.Status = "Confirmed";
            saga.Status = "Completed";
            saga.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Saga {SagaId}] Korak 4a: Registracija {RegId} postavljena na CONFIRMED.",
                saga.SagaId, registration.RegistrationId);

            // 4b. Potvrdi rezervaciju u EventService
            // (prebacuje je iz privremene SagaSpotReservations u stvarni EventRegistrationTracker)
            try
            {
                await _eventServiceClient.ConfirmSpotAsync(eventId, saga.SagaId, cancellationToken);
                _logger.LogInformation(
                    "[Saga {SagaId}] Korak 4b: EventService potvrdio rezervaciju.", saga.SagaId);
            }
            catch (Exception confirmEx)
            {
                // ConfirmSpot greška nije fatalna za saga - registracija je već potvrđena.
                // Administrativno treba ručno sinhronizovati EventRegistrationTracker.
                _logger.LogError(confirmEx,
                    "[Saga {SagaId}] Korak 4b: ConfirmSpot u EventService pukao. " +
                    "Registracija je CONFIRMED ali EventRegistrationTracker može biti nesinhronizovan. " +
                    "Potrebna ručna korekcija za EventId={EventId}.",
                    saga.SagaId, eventId);
            }

            // 4c. Pošalji email notifikaciju (best-effort, greška se loguje ali ne otkazuje sagu)
            try
            {
                await _emailQueuePublisher.EnqueueAsync(new EmailNotificationMessage
                {
                    RegistrationId = registration.RegistrationId,
                    ParticipantFirstName = participantFirstName,
                    ParticipantLastName = participantLastName,
                    ParticipantEmail = participantEmail,
                    EventId = eventId,
                    EventName = eventName,
                    EventDateTime = DateTime.MinValue,
                    RegistrationDate = registrationDate
                }, cancellationToken);

                _logger.LogInformation(
                    "[Saga {SagaId}] Korak 4c: Email notifikacija stavljena u red.", saga.SagaId);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx,
                    "[Saga {SagaId}] Korak 4c: Email nije stavljen u red (best-effort - ignorišemo).",
                    saga.SagaId);
            }

            _logger.LogInformation(
                "[Saga {SagaId}] ✅ SAGA ZAVRŠENA USPJEŠNO. RegistrationId={RegId}.",
                saga.SagaId, registration.RegistrationId);

            return new SagaResult(true, registration.RegistrationId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Saga {SagaId}] Korak 4 PUKAO. Pokrećem kompenzacije Koraka 3, 2 i 1.",
                saga.SagaId);

            saga.Status = "Compensating";
            saga.FailureReason = $"Korak 4 pukao: {ex.Message}";
            saga.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await CompensateStep3Async(saga, cancellationToken);
            await CompensateStep2Async(saga, cancellationToken);
            await CompensateStep1Async(saga, $"Korak 4 pukao: {ex.Message}", cancellationToken);
            return new SagaResult(false, null, "Greška pri potvrdi registracije. Registracija je otkazana.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // KOMPENZACIONE METODE
    // Svaka kompenzacija poništava jedan korak Sage.
    // Grešaka kompenzacije loguju se ali NE bacaju iznimke
    // (ne možemo kompenzovati kompenzaciju - samo logujemo i nastavljamo).
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kompenzacija Koraka 1: Briše PENDING registraciju iz baze.
    /// </summary>
    private async Task CompensateStep1Async(SagaState saga, string reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Saga {SagaId}] ↩ Kompenzacija Koraka 1: Brišem PENDING registraciju {RegId}...",
            saga.SagaId, saga.RegistrationId);

        try
        {
            if (saga.RegistrationId.HasValue)
            {
                var reg = await _context.Registrations.FindAsync(
                    new object[] { saga.RegistrationId.Value }, cancellationToken);

                if (reg != null)
                {
                    _context.Registrations.Remove(reg);
                }
            }

            saga.Status = "Compensated";
            saga.FailureReason = reason;
            saga.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[Saga {SagaId}] ↩ Kompenzacija Koraka 1 završena: Registracija {RegId} obrisana.",
                saga.SagaId, saga.RegistrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Saga {SagaId}] ⚠ KOMPENZACIJA KORAKA 1 PUKLA! Registracija {RegId} možda ostaje u bazi. " +
                "Potrebna ručna intervencija!",
                saga.SagaId, saga.RegistrationId);

            saga.Status = "Failed";
            saga.UpdatedAt = DateTime.UtcNow;
            try { await _context.SaveChangesAsync(cancellationToken); } catch { /* nema pomoći */ }
        }
    }

    /// <summary>
    /// Kompenzacija Koraka 2: Otkazuje rezervaciju mjesta u EventService.
    /// </summary>
    private async Task CompensateStep2Async(SagaState saga, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Saga {SagaId}] ↩ Kompenzacija Koraka 2: Otkazujem rezervaciju mjesta u EventService (EventId={EventId})...",
            saga.SagaId, saga.EventId);

        try
        {
            await _eventServiceClient.ReleaseSpotAsync(saga.EventId, saga.SagaId, CancellationToken.None);
            _logger.LogInformation(
                "[Saga {SagaId}] ↩ Kompenzacija Koraka 2 završena: Rezervacija otkazana.", saga.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Saga {SagaId}] ⚠ KOMPENZACIJA KORAKA 2 PUKLA! Rezervacija možda ostaje u EventService. " +
                "Potrebna ručna intervencija za EventId={EventId}!",
                saga.SagaId, saga.EventId);
        }
    }

    /// <summary>
    /// Kompenzacija Koraka 3: Otkazuje evidenciju prisustva u DirectoryService.
    /// </summary>
    private async Task CompensateStep3Async(SagaState saga, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Saga {SagaId}] ↩ Kompenzacija Koraka 3: Otkazujem prisustvo u DirectoryService (LocationId={LocationId})...",
            saga.SagaId, saga.LocationId);

        try
        {
            await _directoryServiceClient.ReleaseAttendanceAsync(
                saga.LocationId, saga.SagaId, CancellationToken.None);

            _logger.LogInformation(
                "[Saga {SagaId}] ↩ Kompenzacija Koraka 3 završena: Prisustvo ukloneno.", saga.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Saga {SagaId}] ⚠ KOMPENZACIJA KORAKA 3 PUKLA! Prisustvo možda ostaje u DirectoryService. " +
                "Potrebna ručna intervencija za LocationId={LocationId}!",
                saga.SagaId, saga.LocationId);
        }
    }
}