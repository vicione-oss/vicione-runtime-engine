using AwesomeAssertions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class DefaultRunModeAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsRunModeToProperty()
        {
            var runMode = RunMode.Cyclic;

            var attribute = new DefaultRunModeAttribute(runMode);

            attribute.RunMode.Should().Be(runMode);
        }
    }
}
