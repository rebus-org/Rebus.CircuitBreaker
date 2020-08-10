using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.CircuitBreaker.Tests.CircuitBreaker
{
    [TestFixture]
    public class CircuitBreakerTests : FixtureBase
    {
        [Test]
        public async Task CircuitBreakerIntegrationTest()
        {
            var network = new InMemNetwork();

            var receiver = Using(new BuiltinHandlerActivator());

            var bus = Configure.With(receiver)
                  .Logging(l => l.Console(minLevel: LogLevel.Debug))
                  .Transport(t => t.UseInMemoryTransport(network, "queue-a"))
                  .Options(o => o.EnableCircuitBreaker(c => c.OpenOn<MyCustomException>(attempts: 1, trackingPeriodInSeconds: 10)))
                  .Start();

            receiver.Handle<string>(async (buss, context, message) => throw new MyCustomException());

            await bus.SendLocal("Uh oh, This is not gonna go well!");

            await Task.Delay(TimeSpan.FromSeconds(5));

            var workerCount = bus.Advanced.Workers.Count;

            Assert.That(workerCount, Is.EqualTo(0), $"Expected worker count to be '0' but was {workerCount}");
        }

        class MyCustomException : Exception { }
    }
}