using Rebus.Config;
using Rebus.Time;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable 1998

namespace Rebus.CircuitBreaker;

class ExceptionTypeCircuitBreaker : ICircuitBreaker
{
    readonly ConcurrentDictionary<long, DateTimeOffset> _errorDates;
    readonly CircuitBreakerSettings settings;
    readonly IRebusTime rebusTime;
    readonly Type exceptionType;

    public ExceptionTypeCircuitBreaker(Type exceptionType, CircuitBreakerSettings settings, IRebusTime rebusTime)
    {
        this.exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
            
        State = CircuitBreakerState.Closed;

        _errorDates = new ConcurrentDictionary<long, DateTimeOffset>();
    }

    public CircuitBreakerState State { get; private set; }

    public bool IsClosed => State == CircuitBreakerState.Closed;

    public bool IsHalfOpen => State == CircuitBreakerState.HalfOpen;

    public bool IsOpen => State == CircuitBreakerState.Open;

    public async Task Trip(Exception exception)
    {
        if (ShouldTripCircuitBreaker(exception) == false)
        {
            return;
        }

        var timeStamp = rebusTime.Now;
        _errorDates.TryAdd(timeStamp.Ticks, timeStamp);

        var errorsInPeriod = _errorDates
            .Where(x => x.Key > timeStamp.Ticks - settings.TrackingPeriod.Ticks)
            .Take(settings.Attempts)
            .ToList();

        var numberOfErrorsInPeriod = errorsInPeriod.Count;

        if (IsInRecoveringState(numberOfErrorsInPeriod))
        {
            State = CircuitBreakerState.Open;
            return;
        }

        // Do the tripping
        if (numberOfErrorsInPeriod >= settings.Attempts)
        {
            State = CircuitBreakerState.Open;
        }

        RemoveOutOfPeriodErrors(errorsInPeriod);

        await Task.FromResult(0);
    }

    public async Task Reset()
    {
        if (IsClosed)
        {
            return;
        }

        var latestError = _errorDates
            .OrderByDescending(x => x.Key)
            .Take(1)
            .FirstOrDefault();

        if (latestError.Equals(default(KeyValuePair<int, DateTimeOffset>)))
            return;

        var currentTime = rebusTime.Now;

        if (currentTime > latestError.Value + settings.HalfOpenResetInterval)
        {
            State = CircuitBreakerState.HalfOpen;
        }

        if (currentTime > latestError.Value + settings.CloseResetInterval)
        {
            State = CircuitBreakerState.Closed;
        }
    }

    bool ShouldTripCircuitBreaker(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            var actualException = aggregateException.InnerExceptions.First();

            if (actualException.GetType() == exceptionType)
            {
                return true;
            }
        }

        if (exception.GetType() == exceptionType)
        {
            return true;
        }

        return false;
    }

    bool IsInRecoveringState(int numberOfErrorsInPeriod)
    {
        return numberOfErrorsInPeriod == 1 && IsHalfOpen;
    }

    void RemoveOutOfPeriodErrors(IEnumerable<KeyValuePair<long, DateTimeOffset>> tripsInPeriod)
    {
        var outDatedTimeStamps = _errorDates
            .Except(tripsInPeriod)
            .ToList();

        foreach (var outDatedTimeStamp in outDatedTimeStamps)
        {
            _errorDates.TryRemove(outDatedTimeStamp.Key, out _);
        }
    }
}
