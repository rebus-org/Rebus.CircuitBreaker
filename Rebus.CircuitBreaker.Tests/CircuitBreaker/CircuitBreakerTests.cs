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
    public async Task OpensCircuitBreakerOnException()
    {
        var bus = ConfigureBus(
            handlers: a => a.Handle<string>(async _ => throw new MyCustomException()),
            options: o => o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(attempts: 1, trackingPeriodInSeconds: 10))
        );

        await bus.SendLocal("Uh oh, This is not gonna go well!");

        await Task.Delay(TimeSpan.FromSeconds(5));

        var workerCount = bus.Advanced.Workers.Count;

        Assert.That(workerCount, Is.EqualTo(0), $"Expected worker count to be '0' but was {workerCount}");
    }

    [Test]
    public async Task OpensCircuitBreakerAgainAfterLittleWhile()
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
            options: o => o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(attempts: 1, trackingPeriodInSeconds: 10, halfOpenPeriodInSeconds: 20, resetIntervalInSeconds: 30))
        );

        await bus.SendLocal("Uh oh, This is not gonna go well!");

        await Task.Delay(TimeSpan.FromSeconds(35));

        var workerCount = bus.Advanced.Workers.Count;

        Assert.That(workerCount, Is.EqualTo(1), $"Expected worker count to be '1' after waiting the entire reset interval plus some more, but was {workerCount}");
    }

    IBus ConfigureBus(Action<BuiltinHandlerActivator> handlers, Action<OptionsConfigurer> options)
    {
        var network = new InMemNetwork();
        var activator = Using(new BuiltinHandlerActivator());

        handlers(activator);

        return Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel.Debug))
            .Transport(t => t.UseInMemoryTransport(network, "queue-a"))
            .Options(options)
            .Start();
    }

    class MyCustomException : Exception { }
}