using AwesomeAssertions;
using ViciOne.Core.Contracts.DataModel.Attributes;
using Xunit;

public class DefaultPoolingForTypeAttribute_T_Tests
{
    public class Constructor
    {
        [Fact]
        public void PassesTypeToBase()
        {
            var attribute = new DefaultPoolingForTypeAttribute<int>();

            attribute.Type.Should().BeSameAs(typeof(int));
        }
    }
}
