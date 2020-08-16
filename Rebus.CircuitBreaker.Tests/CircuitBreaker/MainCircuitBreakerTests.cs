using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.TestHelpers;
using Rebus.Threading;
using Rebus.Threading.SystemThreadingTimer;

namespace Rebus.CircuitBreaker.Tests.CircuitBreaker
{
    [TestFixture]
    public class MainCircuitBreakerTests
    {
        IAsyncTaskFactory taskFactory;
        IRebusLoggerFactory rebusLoggerFactory;

        CircuitBreakerEvents circuitBreakerEvents;
        private Lazy<IBus> fakeBus;

        [SetUp]
        public void Setup()
        {
            rebusLoggerFactory = new ConsoleLoggerFactory(false);
            taskFactory = new SystemThreadingTimerAsyncTaskFactory(rebusLoggerFactory);
            circuitBreakerEvents = new CircuitBreakerEvents();
            fakeBus = new Lazy<IBus>(new FakeBus());
        }

        [Test]
        public void State_AllClosed_ReturnsClosed()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
            }, rebusLoggerFactory, taskFactory, fakeBus, circuitBreakerEvents, new Options());


            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Closed));
        }

        [Test]
        public void State_OneHalfOpen_ReturnsHalfOpen()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.HalfOpen),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
            }, rebusLoggerFactory, taskFactory, fakeBus, circuitBreakerEvents, new Options());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.HalfOpen));
        }

        [Test]
        public void State_OneOpen_ReturnsOpen()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Open),
            }, new ConsoleLoggerFactory(false), taskFactory, fakeBus, circuitBreakerEvents, new Options());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
        }

        [Test]
        public void State_MixedBag_ReturnsOpen()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.HalfOpen),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Open),
            }, rebusLoggerFactory, taskFactory, fakeBus, circuitBreakerEvents, new Options());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
        }

        internal class FakeCircuitBreaker : ICircuitBreaker
        {
            public FakeCircuitBreaker(CircuitBreakerState state)
            {
                this.State = state;
            }

            public CircuitBreakerState State { get; private set; }

            public async Task Trip(Exception exception)
            {
                await Task.FromResult(0);
            }

            public async Task Reset()
            {
                await Task.FromResult(0);
            }
        }
    }
}