using AwesomeAssertions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class SettingFactoryTests
{
    public class DesignToSetting
    {
        [Fact]
        public void CreatesSetting()
        {
            var design = new SettingDesign<object>();

            ISetting variable = SettingFactory.DesignToSetting(design);

            variable.Should().NotBeNull();
        }
    }
}
