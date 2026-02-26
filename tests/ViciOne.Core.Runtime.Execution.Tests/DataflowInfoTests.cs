using AwesomeAssertions;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class DataflowInfoTests
{
    public class Constructor
    {
        [Fact]
        public void StoresDataflow()
        {
            var dataflow = new Dataflow();

            var created = new DataflowInfo(dataflow, [], [], [], []);

            created.Dataflow.Should().BeSameAs(dataflow);
        }

        [Fact]
        public void StoresInputs()
        {
            var inputs = new List<InputConnectorInfo>();

            var created = new DataflowInfo(new Dataflow(), inputs, [], [], []);

            created.Inputs.Should().BeSameAs(inputs);
        }

        [Fact]
        public void StoresOutputs()
        {
            var outputs = new List<OutputConnectorInfo>();

            var created = new DataflowInfo(new Dataflow(), [], outputs, [], []);

            created.Outputs.Should().BeSameAs(outputs);
        }

        [Fact]
        public void StoresSettings()
        {
            var settings = new List<ISetting>();

            var created = new DataflowInfo(new Dataflow(), [], [], settings, []);

            created.Settings.Should().BeSameAs(settings);
        }

        [Fact]
        public void StoresPersistentVariables()
        {
            var variables = new List<IPersistentVariable>();

            var created = new DataflowInfo(new Dataflow(), [], [], [], variables);

            created.PersistentVariables.Should().BeSameAs(variables);
        }
    }
}
