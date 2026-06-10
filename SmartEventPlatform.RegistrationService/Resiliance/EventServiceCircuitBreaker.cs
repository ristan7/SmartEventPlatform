namespace SmartEventPlatform.RegistrationService.Resilience;

public sealed class EventServiceCircuitBreaker : ManualCircuitBreaker
{
    public EventServiceCircuitBreaker()
        : base("EventService", failureThreshold: 2, openDuration: TimeSpan.FromSeconds(15))
    {
    }
}