using System;

namespace Rebus.CircuitBreaker.Tests
{
    class DisposableCallback : IDisposable
    {
        readonly Action _callback;

        public DisposableCallback(Action callback) => _callback = callback;

        public void Dispose() => _callback();
    }
}
