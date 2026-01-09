using AwesomeAssertions;
using System;
using ViciOne.Engine;
using Xunit;

public class DefaultReferenceTypeCloneHandlerTests
{
    public class CloneIfNecessary
    {
        [Fact]
        public void CallsCloneFuncIfRegisteredForType()
        {
            var instance = new object();
            using var handler = new DefaultReferenceTypeCloneHandler();
            handler.RegisterHandler((object? _) => new object());

            var returnedInstance = handler.CloneIfNecessary(instance);

            returnedInstance.Should().NotBeSameAs(instance);
        }

        [Fact]
        public void ReturnsSameInstanceIfNotRegisteredForType()
        {
            var instance = new object();
            using var handler = new DefaultReferenceTypeCloneHandler();

            var returnedInstance = handler.CloneIfNecessary(instance);

            returnedInstance.Should().BeSameAs(instance);
        }
    }

    public class Dispose
    {
        [Fact]
        public void CanDispose()
        {
            using var handler = new DefaultReferenceTypeCloneHandler();

            Action action = handler.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            using var handler = new DefaultReferenceTypeCloneHandler();
            handler.Dispose();

            Action action = handler.Dispose;

            action.Should().NotThrow();
        }
    }
}
