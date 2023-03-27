using System;
using System.Threading.Tasks;

namespace Rebus.CircuitBreaker;

interface ICircuitBreaker
{
    CircuitBreakerState State { get; }
    Task Trip(Exception exception);
    Task Reset();
}