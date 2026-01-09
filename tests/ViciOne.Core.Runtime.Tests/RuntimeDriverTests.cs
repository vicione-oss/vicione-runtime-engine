using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using ViciOne.Core.Runtime;
using Xunit;

public class RuntimeDriverTests
{
    public class Setup
    {
        [Fact]
        public void CanSetup()
        {
            using var driver = new DummyDriver();
            var logger = Substitute.For<ILogger>();

            Action act = () => driver.Setup(logger);

            act.Should().NotThrow();
        }

        [Fact]
        public void ThrowsIfCalledAgain()
        {
            using var driver = new DummyDriver();
            var logger = Substitute.For<ILogger>();
            driver.Setup(logger);

            Action act = () => driver.Setup(logger);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not*allowed*");
        }

        [Fact]
        public void StoresLogger()
        {
            using var driver = new DummyDriver();
            var logger = Substitute.For<ILogger>();

            driver.Setup(logger);

            driver.Logger.Should().BeSameAs(logger);
        }

        private sealed class DummyDriver : RuntimeDriver<DummyDriver>
        {
            public new ILogger Logger => base.Logger;
        }
    }
}
