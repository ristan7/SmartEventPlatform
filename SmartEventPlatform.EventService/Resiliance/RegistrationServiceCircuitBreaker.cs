namespace SmartEventPlatform.EventService.Resilience;

public sealed class RegistrationServiceCircuitBreaker : ManualCircuitBreaker
{
    public RegistrationServiceCircuitBreaker()
        : base("RegistrationService", failureThreshold: 3, openDuration: TimeSpan.FromSeconds(20))
    {
    }
}