using System;

namespace Rebus.CircuitBreaker;

/// <summary>
/// Has events that can be subscribed to if one wants to be notified when certain things happen
/// </summary>
public class CircuitBreakerEvents
{
    /// <summary>
    /// Event that is raised when the circuit breaker is changing state. <see cref="CircuitBreakerState"/>
    /// </summary>
    public event Action<CircuitBreakerState> CircuitBreakerChanged;

    internal void RaiseCircuitBreakerChanged(CircuitBreakerState state)
    {
        CircuitBreakerChanged?.Invoke(state);
    }
}