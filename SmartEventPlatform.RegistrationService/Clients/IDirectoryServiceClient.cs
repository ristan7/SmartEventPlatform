namespace SmartEventPlatform.RegistrationService.Clients;

public interface IDirectoryServiceClient
{
    // Korak 3: Zabilježi prisustvo na lokaciji (increment)
    Task RecordAttendanceAsync(long locationId, long sagaId, CancellationToken cancellationToken);

    // Korak 3 kompenzacija: Otkaži prisustvo (decrement)
    Task ReleaseAttendanceAsync(long locationId, long sagaId, CancellationToken cancellationToken);
}