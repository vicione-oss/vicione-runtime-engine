using AwesomeAssertions;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class DefaultValueAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsValueToProperty()
        {
            var value = 42;

            var attribute = new DefaultValueAttribute(value);

            attribute.Value.Should().Be(value);
        }
    }
}
