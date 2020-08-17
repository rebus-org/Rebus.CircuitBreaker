using System;
using System.Threading.Tasks;

namespace Rebus.CircuitBreaker
{
    internal interface ICircuitBreaker
    {
        CircuitBreakerState State { get; }
        Task Trip(Exception exception);
        Task Reset();
    }
}
