namespace SmartEventPlatform.RegistrationService.Clients;

public sealed class DirectoryServiceClient : IDirectoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DirectoryServiceClient> _logger;

    public DirectoryServiceClient(HttpClient httpClient, ILogger<DirectoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task RecordAttendanceAsync(long locationId, long sagaId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(
            $"/api/saga/locations/{locationId}/record-attendance?sagaId={sagaId}",
            null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "[Saga {SagaId}] DirectoryService vratio {Code} za RecordAttendance (LocationId={LocationId}).",
                sagaId, (int)response.StatusCode, locationId);

            response.EnsureSuccessStatusCode();
        }
    }

    public async Task ReleaseAttendanceAsync(long locationId, long sagaId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(
            $"/api/saga/locations/{locationId}/release-attendance?sagaId={sagaId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "[Saga {SagaId}] DirectoryService nije pronašao evidenciju prisustva za lokaciju {LocationId}. " +
                "Nastavaljamo kompenzaciju.",
                sagaId, locationId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "[Saga {SagaId}] DirectoryService vratio {Code} za ReleaseAttendance (LocationId={LocationId}). " +
                "Kompenzacija možda nije potpuna.",
                sagaId, (int)response.StatusCode, locationId);
        }
    }
}