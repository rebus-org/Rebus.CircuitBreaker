using Rebus.Bus;
using Rebus.Logging;
using Rebus.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Config;

namespace Rebus.CircuitBreaker
{
    internal class MainCircuitBreaker : IInitializable, IDisposable
    {
        const string BackgroundTaskName = "CircuitBreakersResetTimer";

        readonly IAsyncTask _resetCircuitBreakerTask;

        readonly IList<ICircuitBreaker> _circuitBreakers;
        readonly CircuitBreakerEvents _circuitBreakerEvents;
        readonly Func<IBus> _busGetter;
        readonly Options _options;
        readonly ILog _log;

        int _configuredNumberOfWorkers;

        bool _disposed;

        IBus _bus;
        IBus Bus => _bus ?? (_bus = _busGetter());

        public MainCircuitBreaker(IList<ICircuitBreaker> circuitBreakers, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory, Func<IBus> busGetter, CircuitBreakerEvents circuitBreakerEvents, Options options)
        {
            _circuitBreakers = circuitBreakers ?? new List<ICircuitBreaker>();
            _log = rebusLoggerFactory?.GetLogger<MainCircuitBreaker>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _busGetter = busGetter;
            _circuitBreakerEvents = circuitBreakerEvents;
            _options = options;

            _resetCircuitBreakerTask = asyncTaskFactory.Create(BackgroundTaskName, Reset, prettyInsignificant: false, intervalSeconds: 1);
        }

        public void Initialize()
        {
            _log.Info("Initializing circuit breaker");

            _configuredNumberOfWorkers = _options.NumberOfWorkers;
            
            _resetCircuitBreakerTask.Start();
        }

        public CircuitBreakerState State => _circuitBreakers.Aggregate(CircuitBreakerState.Closed, (currentState, incoming) =>
        {
            if (incoming.State > currentState)
            {
                return incoming.State;
            }

            return currentState;
        });

        public bool IsClosed => _circuitBreakers.All(x => x.State == CircuitBreakerState.Closed);

        public bool IsHalfOpen => State == CircuitBreakerState.HalfOpen;

        public bool IsOpen => State == CircuitBreakerState.Open;

        public void Trip(Exception exception)
        {
            var previousState = State;

            foreach (var circuitBreaker in _circuitBreakers)
            {
                circuitBreaker.Trip(exception);
            }

            if (previousState == State)
            {
                return;
            }

            _log.Info("Circuit breaker changed from {PreviousState} to {State}", previousState, State);
            _circuitBreakerEvents.RaiseCircuitBreakerChanged(State);

            var workers = Bus.Advanced.Workers;

            if (IsClosed)
            {
                workers.SetNumberOfWorkers(_configuredNumberOfWorkers);
                return;
            }


            if (IsHalfOpen)
            {
                workers.SetNumberOfWorkers(1);
                return;
            }

            if (IsOpen)
            {
                workers.SetNumberOfWorkers(0);
                return;
            }
        }

        public async Task Reset()
        {
            foreach (var circuitBreaker in _circuitBreakers)
            {
                await circuitBreaker.Reset();
            }
        }

        /// <summary>
        /// Last-resort disposal of reset circuit breaker reset timer
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _resetCircuitBreakerTask.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
