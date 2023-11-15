using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry;
using Rebus.TestHelpers;
using Rebus.Threading;
using Rebus.Threading.SystemThreadingTimer;

namespace Rebus.CircuitBreaker.Tests.CircuitBreaker;

[TestFixture]
public class CircuitBreakerErrorTrackerTests
{
    ConsoleLoggerFactory rebusLoggerFactory;
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

    MainCircuitBreaker EmptyCircuitBreaker() => new(new List<ICircuitBreaker>()
        , rebusLoggerFactory
        , taskFactory
        , fakeBus
        , circuitBreakerEvents, new Options());

    [Test]
    public async Task RegisterError_Should_Register_Exception_In_Inner_ErrorTracker()
    {
        var emptyCircuitBreaker = EmptyCircuitBreaker();
        var exceptionRegisteredInInnerErrorHandler = false;
        var errorTrackerStub = new TestableErrorTrackerStub((messageId, exception) => exceptionRegisteredInInnerErrorHandler = true);
        var sut = new CircuitBreakerErrorTracker(errorTrackerStub, emptyCircuitBreaker);

        await sut.RegisterError(Guid.NewGuid().ToString(), new Exception(":/"));

        Assert.True(exceptionRegisteredInInnerErrorHandler);
    }

    class TestableErrorTrackerStub : IErrorTracker
    {
        readonly Action<string, Exception> _registerErrorCallBack;

        public TestableErrorTrackerStub(Action<string, Exception> registerErrorCallBack = null)
        {
            _registerErrorCallBack = registerErrorCallBack;
        }

        public async Task RegisterError(string messageId, Exception exception)
        {
            _registerErrorCallBack?.Invoke(messageId, exception);
        }

        public Task CleanUp(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasFailedTooManyTimes(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFullErrorDescription(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<ExceptionInfo>> GetExceptions(string messageId)
        {
            throw new NotImplementedException();
        }

        public Task MarkAsFinal(string messageId)
        {
            throw new NotImplementedException();
        }
    }
}