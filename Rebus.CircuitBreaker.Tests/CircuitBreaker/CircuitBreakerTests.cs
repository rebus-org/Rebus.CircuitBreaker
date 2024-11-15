using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
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
        var deliveryCount = 0;

        var bus = ConfigureBus(
            handlers: a => a.Handle<string>(async _ =>
            {
                deliveryCount++;
                throw new MyCustomException();
            }),
            options: o =>
            {
                o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(attempts: 1, trackingPeriodInSeconds: 10, halfOpenPeriodInSeconds: 20, resetIntervalInSeconds: 30));
            },
            useBusStarter
        );

        await bus.SendLocal("Uh oh, This is not gonna go well!");

        await Task.Delay(TimeSpan.FromSeconds(10));
        Assert.That(deliveryCount, Is.EqualTo(1), $"Expect message delivery count to be '1' after circuit has transitioned from closed -> open");

        await Task.Delay(TimeSpan.FromSeconds(20));
        Assert.That(deliveryCount, Is.EqualTo(2), $"Expect message delivery count to be '2' after circuit has cycled from closed -> open -> halfopen -> open");
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
