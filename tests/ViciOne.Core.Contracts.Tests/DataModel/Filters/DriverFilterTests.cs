using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Filters;
using Xunit;

public class DriverFilterTests
{
    public class GetDistinctRuntimeDriverTypeNames
    {
        [Fact]
        public void GetsDistinctRuntimeDriverTypeNames()
        {
            var blockWithDriver1 = CreateBlockWithDriver("AA");
            var blockWithDriver2 = CreateBlockWithDriver("AA");
            var blockWithDriver3 = CreateBlockWithDriver("BB");
            IFunctionBlock[] all = [blockWithDriver1, blockWithDriver2, blockWithDriver3];
            string[] expectedDistinct = [blockWithDriver1.Design.RuntimeDriverTypeName!, blockWithDriver3.Design.RuntimeDriverTypeName!];

            var distinct = DriverFilter.GetDistinctRuntimeDriverTypeNames(all);

            distinct.Should().BeEquivalentTo(expectedDistinct);
        }
    }

    private static IFunctionBlock CreateBlockWithDriver(string typeName)
    {
        var block = Substitute.For<IFunctionBlock>();
        block.Design.RuntimeDriverTypeName.Returns(typeName);
        return block;
    }
}
