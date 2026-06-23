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
}
