using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry.Simple;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleAnonymousFunction
#pragma warning disable 1998

namespace Rebus.CircuitBreaker.Tests.CircuitBreaker;

[TestFixture]
public class CircuitBreakerTests : FixtureBase
{
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task OpensCircuitBreakerOnException(bool useBusStarter)
    {
        var bus = ConfigureBus(
            handlers: a => a.Handle<string>(async _ => throw new MyCustomException()),
            options: o => o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(attempts: 1, trackingPeriodInSeconds: 10)),
            useBusStarter
            );

        await bus.SendLocal("Uh oh, This is not gonna go well!");

        await Task.Delay(TimeSpan.FromSeconds(5));

        var workerCount = bus.Advanced.Workers.Count;

        Assert.That(workerCount, Is.EqualTo(0), $"Expected worker count to be '0' but was {workerCount}");
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task OpensCircuitBreakerAgainAfterLittleWhile(bool useBusStarter)
    {
        var deliveryCount = 0;

        var bus = ConfigureBus(
            handlers: a => a.Handle<string>(async _ =>
            {
                deliveryCount++;

                if (deliveryCount > 1)
                {
                    Console.WriteLine($"Handling message properly this time");
                    return;
                }

                throw new MyCustomException();
            }),
            options: o =>
            {
                o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(attempts: 1, trackingPeriodInSeconds: 10, halfOpenPeriodInSeconds: 20, resetIntervalInSeconds: 30));
            },
            useBusStarter
        );

        await bus.SendLocal("Uh oh, This is not gonna go well!");

        await Task.Delay(TimeSpan.FromSeconds(35));

        var workerCount = bus.Advanced.Workers.Count;

        Assert.That(workerCount, Is.EqualTo(1), $"Expected worker count to be '1' after waiting the entire reset interval plus some more, but was {workerCount}");
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task WaitHalfOpenPeriodBeforeHalfOpening(bool useBusStarter)
    {
        var stateChanges = new List<CircuitBreakerState>();
        var events = new CircuitBreakerEvents();
        events.CircuitBreakerChanged += (CircuitBreakerState newState) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Circuit breaker state changed {stateChanges.LastOrDefault()} -> {newState}");
            stateChanges.Add(newState);
        };

        var deliveryCount = 0;

        var bus = ConfigureBus(
            handlers: a => a.Handle<string>(async _ =>
            {
                deliveryCount++;
                await Task.Delay(500); // Make handler take reasonable amount of time.
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] DeliveryCount: {deliveryCount}");
                throw new MyCustomException();
            }),
            options: o =>
            {
                o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(attempts: 1, trackingPeriodInSeconds: 10, halfOpenPeriodInSeconds: 20, resetIntervalInSeconds: 30));
                o.Decorate(c => events);
            },
            useBusStarter
        );

        await bus.SendLocal("Uh oh, This is not gonna go well!");

        await Task.Delay(TimeSpan.FromSeconds(10));
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Assert state changes -> open");
        Assert.That(stateChanges, Is.EquivalentTo(new CircuitBreakerState[] { CircuitBreakerState.Open }), $"[{DateTime.Now:HH:mm:ss.fff}] Expect state transition -> open after first error but before halfOpenPeriod");

        await Task.Delay(TimeSpan.FromSeconds(20));
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Assert state changes -> open -> half open -> open");
        Assert.That(stateChanges, Is.EquivalentTo(new CircuitBreakerState[] { CircuitBreakerState.Open, CircuitBreakerState.HalfOpen, CircuitBreakerState.Open }), $"[{DateTime.Now:HH:mm:ss.fff}] Expect state transitions -> open -> half open -> open after halfOpenPeriod");
    }

    IBus ConfigureBus(Action<BuiltinHandlerActivator> handlers, Action<OptionsConfigurer> options, bool useBusStarter)
    {
        var network = new InMemNetwork();
        var activator = Using(new BuiltinHandlerActivator());

        handlers(activator);

        var configurer = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel.Debug))
            .Transport(t => t.UseInMemoryTransport(network, "queue-a"))
            .Options(options);

        if (useBusStarter)
        {
            var starter = configurer.Create();
            return starter.Start();
        }

        return configurer.Start();
    }

    class MyCustomException : Exception { }
}