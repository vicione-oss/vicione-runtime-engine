using AwesomeAssertions;
using System;
using ViciOne.Engine;
using Xunit;

public class EngineQueueOptionsTests
{
    public class CommunicationReceiveBufferCapacity
    {
        [Fact]
        public void ThrowsIfSettingValueLessThanOne()
        {
            var action = () => _ = new EngineQueueOptions() { CommunicationReceiveBufferCapacity = 0 };

            action.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    public class CrossSourceQueueCapacityPerLink
    {
        [Fact]
        public void ThrowsIfSettingValueLessThanOne()
        {
            var action = () => _ = new EngineQueueOptions() { CrossSourceQueueCapacityPerLink = 0 };

            action.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    public class PerLinkQueueCapacity
    {
        [Fact]
        public void ThrowsIfSettingValueLessThanOne()
        {
            var action = () => _ = new EngineQueueOptions() { PerLinkQueueCapacity = 0 };

            action.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
