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
            _log = rebusLoggerFactory?.GetLogger<MainCircuitBreaker>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _circuitBreakers = circuitBreakers ?? new List<ICircuitBreaker>();
            _circuitBreakerEvents = circuitBreakerEvents;
            _busGetter = busGetter;
            _options = options;

            _resetCircuitBreakerTask = asyncTaskFactory.Create(BackgroundTaskName, Reset, prettyInsignificant: false, intervalSeconds: 2);
        }

        public void Initialize()
        {
            _configuredNumberOfWorkers = _options.NumberOfWorkers;

            _log.Info("Initializing circuit breaker with default number of workers = {count}", _configuredNumberOfWorkers);

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

        public void Trip(Exception exception)
        {
            var previousState = State;

            foreach (var circuitBreaker in _circuitBreakers)
            {
                circuitBreaker.Trip(exception);
            }

            var currentState = State;

            if (previousState == currentState)
            {
                return;
            }

            _log.Info("Circuit breaker changed from {PreviousState} to {State}", previousState, currentState);
            _circuitBreakerEvents.RaiseCircuitBreakerChanged(currentState);

            if (currentState == CircuitBreakerState.Closed)
            {
                SetNumberOfWorkers(_configuredNumberOfWorkers);
                return;
            }

            if (currentState == CircuitBreakerState.HalfOpen)
            {
                SetNumberOfWorkers(1);
                return;
            }

            if (currentState == CircuitBreakerState.Open)
            {
                SetNumberOfWorkers(0);
                return;
            }
        }

        void SetNumberOfWorkers(int count)
        {
            _log.Info("Setting number of workers to {count}", count);

            var workers = Bus.Advanced.Workers;

            workers.SetNumberOfWorkers(count);
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
