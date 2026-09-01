using AwesomeAssertions;
using System;
using ViciOne.Engine;
using Xunit;

public class DefaultConnectorValueTransferInterceptorTests
{
    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var handler = new DefaultConnectorValueTransferInterceptor();

            Action action = handler.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var handler = new DefaultConnectorValueTransferInterceptor();
            handler.Dispose();

            Action action = handler.Dispose;

            action.Should().NotThrow();
        }
    }

    public class Intercept
    {
        [Fact]
        public void ReturnsPassedValueIfNotRegisteredForId()
        {
            var valueToPass = new object();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();

            var result = interceptor.Intercept(id, valueToPass);

            result.Value.Should().BeSameAs(valueToPass);
        }

        [Fact]
        public void DoesNotSetCancelFlagIfNotRegisteredForId()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();

            var result = interceptor.Intercept(id, default(object));

            result.CancelValueTransfer.Should().BeFalse();
        }

        [Fact]
        public void CallsInterceptFuncIfRegisteredForId()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            var called = false;
            interceptor.RegisterInterceptor(id, (object? _) =>
            {
                called = true;
                return new DefaultConnectorValueTransferInterceptionResult<object>();
            });

            interceptor.Intercept(id, default(object));

            called.Should().BeTrue();
        }

        [Fact]
        public void PassesValueToFuncIfRegisteredForId()
        {
            var valueToPass = new object();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            object? passedValue = null;
            DefaultConnectorValueTransferInterceptionResult<object> Func(object? arg)
            {
                passedValue = arg;
                return new DefaultConnectorValueTransferInterceptionResult<object>();
            }
            interceptor.RegisterInterceptor<object>(id, Func);

            interceptor.Intercept(id, valueToPass);

            passedValue.Should().BeSameAs(valueToPass);
        }

        [Fact]
        public void ReturnsFuncResultIfRegisteredForId()
        {
            var instance = new object();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            var funcResult = new DefaultConnectorValueTransferInterceptionResult<object>();
            interceptor.RegisterInterceptor(id, (object? _) => funcResult);

            var result = interceptor.Intercept(id, instance);

            result.Should().BeSameAs(funcResult);
        }

        [Fact]
        public void ResetsPendingInterceptionFlag()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            interceptor.SignalInterceptionPending(id);

            interceptor.Intercept(id, default(object));

            var pending = interceptor.IsInterceptionPending(id);
            pending.Should().BeFalse();
        }
    }

    public class IsInterceptionPending
    {
        [Fact]
        public void ReturnsTrueIfInterceptionPending()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            interceptor.RegisterInterceptor(id, (object? _) => new DefaultConnectorValueTransferInterceptionResult<object>());
            interceptor.SignalInterceptionPending(id);

            var pending = interceptor.IsInterceptionPending(id);

            pending.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfNoInterceptionPending()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();

            var pending = interceptor.IsInterceptionPending(id);

            pending.Should().BeFalse();
        }
    }

    public class RegisterInterceptor
    {
        [Fact]
        public void ReplacesPreviouslyRegisteredFunc()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            var calledFirst = false;
            var calledSecond = false;
            DefaultConnectorValueTransferInterceptionResult<object> FirstFunc(object? _)
            {
                calledFirst = true;
                return new DefaultConnectorValueTransferInterceptionResult<object>();
            }
            DefaultConnectorValueTransferInterceptionResult<object> SecondFunc(object? _)
            {
                calledSecond = true;
                return new DefaultConnectorValueTransferInterceptionResult<object>();
            }

            interceptor.RegisterInterceptor<object>(id, FirstFunc);
            interceptor.RegisterInterceptor<object>(id, SecondFunc);
            interceptor.Intercept(id, default(object));

            calledFirst.Should().BeFalse();
            calledSecond.Should().BeTrue();
        }
    }

    public class SignalInterceptionPending
    {
        [Fact]
        public void SignalsOnlyIfInterceptorRegistered()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();

            interceptor.SignalInterceptionPending(id);

            var pending = interceptor.IsInterceptionPending(id);
            pending.Should().BeFalse();
        }
    }

    public class UnregisterInterceptor
    {
        [Fact]
        public void UnregistersInterceptor()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            var called = false;
            interceptor.RegisterInterceptor<object>(id, (_) =>
            {
                called = true;
                return new DefaultConnectorValueTransferInterceptionResult<object>();
            });

            interceptor.UnregisterInterceptor(id);
            interceptor.Intercept(id, default(object));

            called.Should().BeFalse();
        }

        [Fact]
        public void SignalsInterceptionPendingAfterUnregistered()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();
            interceptor.RegisterInterceptor(id, (object? _) => new DefaultConnectorValueTransferInterceptionResult<object>());

            interceptor.UnregisterInterceptor(id);

            var pending = interceptor.IsInterceptionPending(id);
            pending.Should().BeTrue();
        }

        [Fact]
        public void SignalsInterceptionPendingOnlyIfPreviousRegistered()
        {
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            var id = Guid.NewGuid();

            interceptor.UnregisterInterceptor(id);

            var pending = interceptor.IsInterceptionPending(id);
            pending.Should().BeFalse();
        }
    }
}
