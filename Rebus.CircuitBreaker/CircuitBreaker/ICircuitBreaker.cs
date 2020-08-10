using System;
using System.Threading.Tasks;

namespace Rebus.CircuitBreaker
{
    internal interface ICircuitBreaker
    {
        CircuitBreakerState State { get; }
        void Trip(Exception exception);
        Task Reset();
    }
}
