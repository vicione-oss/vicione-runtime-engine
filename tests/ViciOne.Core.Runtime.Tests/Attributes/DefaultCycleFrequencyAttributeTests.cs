using AwesomeAssertions;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class DefaultCycleFrequencyAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsCycleFrequencyToProperty()
        {
            uint cycleFrequency = 42;

            var attribute = new DefaultCycleFrequencyAttribute(cycleFrequency);

            attribute.CycleFrequency.Should().Be(cycleFrequency);
        }
    }
}
