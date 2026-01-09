using AwesomeAssertions;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class DefaultValueAttribute_T_Tests
{
    public class Constructor
    {
        [Fact]
        public void CreatesInstanceOfT()
        {
            var attribute = new DefaultValueAttribute<Custom>();

            attribute.Value.Should().BeOfType<Custom>();
        }

        private sealed class Custom { }
    }
}
