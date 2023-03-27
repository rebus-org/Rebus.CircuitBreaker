using Rebus.Bus;
using Rebus.CircuitBreaker;
using Rebus.Logging;
using Rebus.Retry;
using Rebus.Threading;
using Rebus.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Injection;

namespace Rebus.Config;

/// <summary>
/// Configuration extensions for the Circuit breakers
/// </summary>
public static class CircuitBreakerConfigurationExtensions
{
    /// <summary>
    /// Enabling fluent configuration of circuit breakers
    /// </summary>
    /// <param name="configurer"></param>
    /// <param name="circuitBreakerBuilder"></param>
    public static void EnableCircuitBreaker(this OptionsConfigurer configurer, Action<CircuitBreakerConfigurationBuilder> circuitBreakerBuilder)
    {
        var builder = new CircuitBreakerConfigurationBuilder();
        circuitBreakerBuilder?.Invoke(builder);

        configurer.Register(context => new CircuitBreakerEvents());

        configurer.Register(context =>
        {
            var loggerFactory = context.Get<IRebusLoggerFactory>();
            var asyncTaskFactory = context.Get<IAsyncTaskFactory>();
            var circuitBreakerEvents = context.Get<CircuitBreakerEvents>();
            var options = context.Get<Options>();
            var circuitBreakers = builder.Build(context);

            return new MainCircuitBreaker(circuitBreakers, loggerFactory, asyncTaskFactory, new Lazy<IBus>(context.Get<IBus>), circuitBreakerEvents, options);
        });

        configurer.Decorate<IErrorTracker>(context =>
        {
            var innerErrorTracker = context.Get<IErrorTracker>();
            var circuitBreaker = context.Get<MainCircuitBreaker>();

            return new CircuitBreakerErrorTracker(innerErrorTracker, circuitBreaker);
        });
    }

    /// <summary>
    /// Configuration builder to fluently register circuit breakers
    /// </summary>
    public class CircuitBreakerConfigurationBuilder
    {
        readonly List<Func<IResolutionContext, ICircuitBreaker>> _circuitBreakerFactories = new List<Func<IResolutionContext, ICircuitBreaker>>();

        /// <summary>
        /// Register a circuit breaker based on an <typeparamref name="TException"/>
        /// </summary>
        /// <typeparam name="TException">Exception type to trip the circuit breaker on</typeparam>
        public CircuitBreakerConfigurationBuilder OpenOn<TException>(
            int attempts = CircuitBreakerSettings.DefaultAttempts,
            int trackingPeriodInSeconds = CircuitBreakerSettings.DefaultTrackingPeriodInSeconds,
            int halfOpenPeriodInSeconds = CircuitBreakerSettings.DefaultHalfOpenResetInterval,
            int resetIntervalInSeconds = CircuitBreakerSettings.DefaultCloseResetInterval
        )
            where TException : Exception
        {
            var settings = new CircuitBreakerSettings(attempts, trackingPeriodInSeconds, halfOpenPeriodInSeconds, resetIntervalInSeconds);

            _circuitBreakerFactories.Add(context => new ExceptionTypeCircuitBreaker(typeof(TException), settings, context.Get<IRebusTime>()));

            return this;
        }

        internal IList<ICircuitBreaker> Build(IResolutionContext context) => _circuitBreakerFactories.Select(factory => factory(context)).ToList();
    }
}