using Rebus.Retry;
using System;
using System.Collections.Generic;

namespace Rebus.CircuitBreaker
{
    internal class CircuitBreakerErrorTracker : IErrorTracker
    {
        readonly IErrorTracker _innerErrorTracker;
        readonly MainCircuitBreaker _circuitBreaker;

        public CircuitBreakerErrorTracker(IErrorTracker innerErrorTracker, MainCircuitBreaker circuitBreaker)
        {
            _innerErrorTracker = innerErrorTracker;
            _circuitBreaker = circuitBreaker;
        }

        public void CleanUp(string messageId)
        {
            _innerErrorTracker.CleanUp(messageId);
        }

        public IEnumerable<Exception> GetExceptions(string messageId)
        {
            return _innerErrorTracker.GetExceptions(messageId);
        }

        public string GetFullErrorDescription(string messageId)
        {
            return _innerErrorTracker.GetFullErrorDescription(messageId);
        }

        public string GetShortErrorDescription(string messageId)
        {
            return _innerErrorTracker.GetShortErrorDescription(messageId);
        }

        public bool HasFailedTooManyTimes(string messageId)
        {
            return _innerErrorTracker.HasFailedTooManyTimes(messageId);
        }

        public void MarkAsFinal(string messageId)
        {
            _innerErrorTracker.MarkAsFinal(messageId);
        }

        public void RegisterError(string messageId, Exception exception)
        {
            _circuitBreaker.Trip(exception).Wait();
            _innerErrorTracker.RegisterError(messageId, exception);
        }
    }
}
