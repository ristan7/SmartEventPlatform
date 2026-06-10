namespace SmartEventPlatform.EventService.Resilience;

public sealed class DirectoryServiceCircuitBreaker : ManualCircuitBreaker
{
    public DirectoryServiceCircuitBreaker()
        : base("DirectoryService", failureThreshold: 2, openDuration: TimeSpan.FromSeconds(15))
    {
    }
}