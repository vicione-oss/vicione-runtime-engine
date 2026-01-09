using AwesomeAssertions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class ValidRunModesAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsDefaultValueToProperty()
        {
            RunMode[] validRunModes = [RunMode.Change, RunMode.Cyclic];

            var attribute = new ValidRunModesAttribute(validRunModes);

            attribute.ValidRunModes.Should().BeEquivalentTo(validRunModes);
        }
    }
}
