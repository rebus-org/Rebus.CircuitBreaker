using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Logging;
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

        [SetUp]
        public void Setup()
        {
            rebusLoggerFactory = new ConsoleLoggerFactory(false);
            taskFactory = new SystemThreadingTimerAsyncTaskFactory(rebusLoggerFactory);
            circuitBreakerEvents = new CircuitBreakerEvents();
        }

        [Test]
        public void State_AllClosed_ReturnsClosed()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
            }, rebusLoggerFactory, taskFactory, null, circuitBreakerEvents, new Options());


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
            }, rebusLoggerFactory, taskFactory, null, circuitBreakerEvents, new Options());

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
            }, new ConsoleLoggerFactory(false), taskFactory, null, circuitBreakerEvents, new Options());

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
            }, rebusLoggerFactory, taskFactory, null, circuitBreakerEvents, new Options());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
        }

        internal class FakeCircuitBreaker : ICircuitBreaker
        {
            public FakeCircuitBreaker(CircuitBreakerState state)
            {
                this.State = state;
            }

            public CircuitBreakerState State { get; private set; }

            public void Trip(Exception exception)
            {
            }

            public async Task Reset()
            {
                await Task.FromResult(0);
            }
        }
    }
}