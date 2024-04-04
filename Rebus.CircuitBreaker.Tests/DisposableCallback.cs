using System;

namespace Rebus.CircuitBreaker.Tests;

class DisposableCallback(Action callback) : IDisposable
{
    public void Dispose() => callback();
}