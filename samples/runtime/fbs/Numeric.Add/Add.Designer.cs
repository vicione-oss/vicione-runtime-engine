using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Engine.DefaultPoolings;

namespace Numeric;

[FunctionBlockDesign("bc97fbdb-6023-42b9-917a-1dd11c4eb86a", "f0aa4c9d-ade5-4c6d-aa8f-ffd5c00e593b", "84bc13d4-441a-4366-a9fc-853a2a01ab83")]
public partial class Add : RuntimeFunctionBlock<Add>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("4ac33ded-2b51-46e8-a393-63033ee6144b")]
        [DefaultCrossSourceAggregatingPooling<MinPooling<double>>]
        public IRuntimeConnector<double> Addend1 { get; internal set; }
        [InputConnectorDesign("2ec52c92-1ed2-4dc0-98f6-a420aecbbec0")]
        [DefaultCrossSourceAggregatingPooling<MaxPooling<double>>]
        public IRuntimeConnector<double> Addend2 { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("2590dc44-a34c-4796-9768-3f992a231f15")]
        public IRuntimeConnector<double> Sum { get; internal set; }
    }
}
