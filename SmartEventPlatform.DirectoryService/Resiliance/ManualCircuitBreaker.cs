namespace SmartEventPlatform.DirectoryService.Resilience;

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string? message) : base(message)
    {
    }
}

public class ManualCircuitBreaker
{
    private readonly object _lock = new object();
    private readonly string _downstreamName;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    private DateTime _lastFailureTime = DateTime.MinValue;
    private int _failureCount;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;

    public ManualCircuitBreaker(string downstreamName, int failureThreshold, TimeSpan openDuration)
    {
        _downstreamName = downstreamName;
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
    }

    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open &&
                    DateTime.UtcNow - _lastFailureTime > _openDuration)
                {
                    _state = CircuitBreakerState.HalfOpen;
                }

                return _state;
            }
        }
    }

    public void EnsureCanExecute()
    {
        if (State == CircuitBreakerState.Open)
        {
            throw new CircuitBreakerOpenException($"CircuitBreaker is open. {_downstreamName} is temporarily unavailable.");
        }
    }

    public void MarkSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
        }
    }

    public void MarkFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen || _failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
            }
        }
    }
}