using Rebus.Bus;
using Rebus.Logging;
using Rebus.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Pipeline;

namespace Rebus.CircuitBreaker;

class MainCircuitBreaker : IInitializable, IDisposable
{
    const string BackgroundTaskName = "CircuitBreakersResetTimer";

    readonly IAsyncTask _resetCircuitBreakerTask;

    readonly IList<ICircuitBreaker> _circuitBreakers;
    readonly CircuitBreakerEvents _circuitBreakerEvents;
    readonly Lazy<IBus> _bus;
    readonly ILog _log;

    readonly int _configuredNumberOfWorkers;

    bool _disposed;

    public MainCircuitBreaker(IList<ICircuitBreaker> circuitBreakers, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory, Lazy<IBus> bus, CircuitBreakerEvents circuitBreakerEvents, int initialWorkerCount)
    {
        _log = rebusLoggerFactory?.GetLogger<MainCircuitBreaker>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _circuitBreakers = circuitBreakers ?? new List<ICircuitBreaker>();
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _circuitBreakerEvents = circuitBreakerEvents;
        _configuredNumberOfWorkers = initialWorkerCount;

        _resetCircuitBreakerTask = asyncTaskFactory.Create(BackgroundTaskName, Reset, prettyInsignificant: true, intervalSeconds: 2);
    }

    public void Initialize()
    {
        _log.Info("Initializing circuit breaker with default number of workers = {count}", _configuredNumberOfWorkers);

        _resetCircuitBreakerTask.Start();
    }

    public CircuitBreakerState State => _circuitBreakers.Aggregate(CircuitBreakerState.Closed, (currentState, incoming) => incoming.State > currentState ? incoming.State : currentState);

    public async Task Trip(Exception exception)
    {
        await InvokeCircuitBreakerAction(circuitBreaker => circuitBreaker.Trip(exception));
    }

    async Task InvokeCircuitBreakerAction(Func<ICircuitBreaker, Task> circuitBreakerAction) 
    {
        var previousState = State;

        foreach (var circuitBreaker in _circuitBreakers)
        {
            await circuitBreakerAction(circuitBreaker);
        }

        var currentState = State;
        if (currentState == previousState)
        {
            return;
        }

        ChangeCircuitBreakerState(previousState, currentState);
    }

    void ChangeCircuitBreakerState(CircuitBreakerState previousState, CircuitBreakerState currentState)
    {
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
        }
    }


    void SetNumberOfWorkers(int count)
    {
        _log.Info("Setting number of workers to {count}", count);

        var workers = _bus.Value.Advanced.Workers;

        // if we're currently executing a message handler, we must execute the operation asynchronously,
        // otherwise we'll end up with a deadlock
        if (MessageContext.Current == null)
        {
            workers.SetNumberOfWorkers(count);
        }
        else
        {
            Task.Run(() => workers.SetNumberOfWorkers(count));
        }
    }

    public async Task Reset()
    {
        await InvokeCircuitBreakerAction(circuitBreaker => circuitBreaker.Reset());
    }

    /// <summary>
    /// disposal of reset circuit breaker reset timer
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