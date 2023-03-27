using Rebus.Retry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.CircuitBreaker;

class CircuitBreakerErrorTracker : IErrorTracker
{
    readonly IErrorTracker _innerErrorTracker;
    readonly MainCircuitBreaker _circuitBreaker;

    public CircuitBreakerErrorTracker(IErrorTracker innerErrorTracker, MainCircuitBreaker circuitBreaker)
    {
        _innerErrorTracker = innerErrorTracker;
        _circuitBreaker = circuitBreaker;
    }

    public Task CleanUp(string messageId) => _innerErrorTracker.CleanUp(messageId);

    public Task<IReadOnlyList<Exception>> GetExceptions(string messageId) => _innerErrorTracker.GetExceptions(messageId);

    public Task<string> GetFullErrorDescription(string messageId) => _innerErrorTracker.GetFullErrorDescription(messageId);

    public Task<bool> HasFailedTooManyTimes(string messageId) => _innerErrorTracker.HasFailedTooManyTimes(messageId);

    public Task MarkAsFinal(string messageId) => _innerErrorTracker.MarkAsFinal(messageId);

    public async Task RegisterError(string messageId, Exception exception)
    {
        await _circuitBreaker.Trip(exception);

        await _innerErrorTracker.RegisterError(messageId, exception);
    }
}