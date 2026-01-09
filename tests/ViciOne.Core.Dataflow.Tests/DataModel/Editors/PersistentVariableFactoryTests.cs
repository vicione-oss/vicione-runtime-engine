using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class PersistentVariableFactoryTests
{
    public class DesignToPersistentVariable
    {
        [Fact]
        public void CreatesVariable()
        {
            var design = new PersistentVariableDesign<object>();

            var variable = PersistentVariableFactory.DesignToPersistentVariable(design);

            variable.Should().NotBeNull();
        }
    }
}
