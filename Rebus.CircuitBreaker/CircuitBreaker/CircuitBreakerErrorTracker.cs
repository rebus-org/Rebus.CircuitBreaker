using Rebus.Retry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.CircuitBreaker;

class CircuitBreakerErrorTracker(IErrorTracker innerErrorTracker, MainCircuitBreaker circuitBreaker) : IErrorTracker
{
    public Task CleanUp(string messageId) => innerErrorTracker.CleanUp(messageId);

    public Task<IReadOnlyList<ExceptionInfo>> GetExceptions(string messageId) => innerErrorTracker.GetExceptions(messageId);

    public Task<string> GetFullErrorDescription(string messageId) => innerErrorTracker.GetFullErrorDescription(messageId);

    public Task<bool> HasFailedTooManyTimes(string messageId) => innerErrorTracker.HasFailedTooManyTimes(messageId);

    public Task MarkAsFinal(string messageId) => innerErrorTracker.MarkAsFinal(messageId);

    public async Task RegisterError(string messageId, Exception exception)
    {
        await circuitBreaker.Trip(exception);

        await innerErrorTracker.RegisterError(messageId, exception);
    }
}