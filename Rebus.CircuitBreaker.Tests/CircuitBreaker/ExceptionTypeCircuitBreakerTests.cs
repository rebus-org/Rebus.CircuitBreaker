using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Config;
using Rebus.TestHelpers;
using Rebus.Time;

namespace Rebus.CircuitBreaker.Tests.CircuitBreaker
{
    [TestFixture]
    public class ExceptionTypeCircuitBreakerTests 
    {
        private IRebusTime time;


        [SetUp]
        public void Setup()
        {
            time = new FakeRebusTime();
        }

        [Test]
        public async Task Trip_WithOneAttemptTrippedOnce_DoesOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(1, 2, 30, 60), time);

            await sut.Trip(new MyCustomException());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
        }

        [Test]
        public async Task Trip_WithTwoAttemptTrippedOnce_DoesNotOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 60, 30, 60), time);

            await sut.Trip(new MyCustomException());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Closed));
        }

        [Test]
        public async Task Trip_WithTwoAttemptTrippedTwice_OutSideTrackingPeriod_DoesNotOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 2, 30, 180), time);

            await sut.Trip(new MyCustomException());
            await Task.Delay(TimeSpan.FromSeconds(3.1));
            await sut.Trip(new MyCustomException());


            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Closed));
        }

        [Test]
        public async Task Trip_WithTwoAttemptTrippedTwice_InsideTrackingPeriod_DoesOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 60, 30, 180), time);

            await sut.Trip(new MyCustomException());
            await Task.Delay(TimeSpan.FromSeconds(2.5));
            await sut.Trip(new MyCustomException());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
        }

        [Test]
        public async Task Trip_IsHalfOpen_DoesOpenCircuit() 
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 2, 4, 10), time);

            // Close The Circuit
            await sut.Trip(new MyCustomException());
            await sut.Trip(new MyCustomException());

            // Wait for half open interval
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Try reset the circuit breaker withing half open internal
            await sut.Reset();
            Assert.IsTrue(sut.IsHalfOpen);

            // Circuit breaker is in recovery state
            // Should the circuit breaker trip, rewind to open state
            await sut.Trip(new MyCustomException());
            Assert.IsTrue(sut.IsOpen);
        }

        [Test]
        public async Task Reset_IsClosed_ShortCircuits() 
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 2, 4, 10), time);
            await sut.Reset();

            Assert.True(sut.IsClosed);
        }

        [Test]
        public async Task Reset_IsWithinHalfOpenResetInterval_IsHalfOpen()
        {
            const int halfOpenResetIntervalInSeconds = 4;

            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(1, 2, halfOpenResetIntervalInSeconds, 10), time);
            await sut.Trip(new MyCustomException());
            Assert.IsTrue(sut.IsOpen);

            await Task.Delay(TimeSpan.FromSeconds(halfOpenResetIntervalInSeconds + 1));
            await sut.Reset();


            Assert.True(sut.IsHalfOpen);
        }

        [Test]
        public async Task Reset_IsWithinClosedResetInterval_IsClosed()
        {
            const int closedResetIntervalInSeconds = 6;
            
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(1, 2, 4, closedResetIntervalInSeconds), time);
            await sut.Trip(new MyCustomException());
            Assert.IsTrue(sut.IsOpen);

            await Task.Delay(TimeSpan.FromSeconds(closedResetIntervalInSeconds + 1));
            await sut.Reset();

            Assert.True(sut.IsClosed);
        }

        class MyCustomException : Exception
        {

        }
    }
}