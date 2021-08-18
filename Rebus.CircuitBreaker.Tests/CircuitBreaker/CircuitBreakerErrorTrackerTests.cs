using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry;
using Rebus.TestHelpers;
using Rebus.Threading;
using Rebus.Threading.SystemThreadingTimer;

namespace Rebus.CircuitBreaker.Tests.CircuitBreaker
{
    [TestFixture]
    public class CircuitBreakerErrorTrackerTests
    {
        private ConsoleLoggerFactory rebusLoggerFactory;
        IAsyncTaskFactory taskFactory;
        Lazy<IBus> fakeBus;
        CircuitBreakerEvents circuitBreakerEvents;

        [SetUp]
        public void Setup()
        {
            rebusLoggerFactory = new ConsoleLoggerFactory(false);
            taskFactory = new SystemThreadingTimerAsyncTaskFactory(rebusLoggerFactory);
            circuitBreakerEvents = new CircuitBreakerEvents();
            fakeBus = new Lazy<IBus>(new FakeBus());
        }

        private MainCircuitBreaker EmptyCircuitBreaker() => new MainCircuitBreaker(new List<ICircuitBreaker>()
            , rebusLoggerFactory
            , taskFactory
            , fakeBus
            , circuitBreakerEvents, new Options());

        [Test]
        public void RegisterError_Should_Register_Exception_In_Inner_ErrorTracker()
        {
            var emptyCircuitBreaker = EmptyCircuitBreaker();
            var exceptionRegisteredInInnerErrorHandler = false;
            var errorTrackerStub = new TestableErrorTrackerStub((messageId, exception) => exceptionRegisteredInInnerErrorHandler = true);
            var sut = new CircuitBreakerErrorTracker(errorTrackerStub, emptyCircuitBreaker);
            
            sut.RegisterError(Guid.NewGuid().ToString(), new Exception(":/"));
            
            Assert.True(exceptionRegisteredInInnerErrorHandler);
        }
        
        private class TestableErrorTrackerStub  : IErrorTracker
        {
            private readonly Action<string, Exception> _registerErrorCallBack;

            public TestableErrorTrackerStub(Action<string, Exception> registerErrorCallBack = null)
            {
                _registerErrorCallBack = registerErrorCallBack;
            }
            
            public void RegisterError(string messageId, Exception exception)
            {
                _registerErrorCallBack?.Invoke(messageId, exception);
            }

            public void CleanUp(string messageId)
            {
                throw new NotImplementedException();
            }

            public bool HasFailedTooManyTimes(string messageId)
            {
                throw new NotImplementedException();
            }

            public string GetShortErrorDescription(string messageId)
            {
                throw new NotImplementedException();
            }

            public string GetFullErrorDescription(string messageId)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Exception> GetExceptions(string messageId)
            {
                throw new NotImplementedException();
            }

            public void MarkAsFinal(string messageId)
            {
                throw new NotImplementedException();
            }
        }
    }
}