using AwesomeAssertions;
using ViciOne.Core.Contracts.DataModel.Attributes;
using Xunit;

public class DefaultPoolingForTypeAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void StoresType()
        {
            var type = typeof(int);

            var attribute = new DefaultPoolingForTypeAttribute(type);

            attribute.Type.Should().BeSameAs(type);
        }
    }
}
