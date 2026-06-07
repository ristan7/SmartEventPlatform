namespace SmartEventPlatform.RegistrationService.Patterns;

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreaker
{
    private readonly object _lock = new object();

    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    private DateTime _lastFailureTime = DateTime.MinValue;
    private int _failureCount;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;

    public CircuitBreaker(int failureThreshold, TimeSpan openDuration)
    {
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

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (State == CircuitBreakerState.Open)
        {
            throw new CircuitBreakerOpenException("CircuitBreaker is open. EventService is temporarily unavailable.");
        }

        try
        {
            var result = await action();

            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitBreakerState.Closed;
            }

            return result;
        }
        catch
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _state = CircuitBreakerState.Open;
                }

                if (_failureCount > _failureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                }
            }

            throw;
        }
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string? message) : base(message)
    {
    }
}