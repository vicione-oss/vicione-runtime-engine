using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;

namespace Logic;

[FunctionBlockDesign("2694b716-af3c-4025-80af-dee0107c7942", "b984fe66-4318-4646-b627-997a7130a939", "ce693f9b-b35f-444e-bad1-a52e049b1c36")]
public partial class AND : RuntimeFunctionBlock<AND>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("4c6edc6b-74a6-4ef7-aedf-d1102a9aa814")]
        public IRuntimeConnector<bool> A { get; internal set; }
        [InputConnectorDesign("b6aa1736-42ec-4078-b02f-af65376afd31")]
        public IRuntimeConnector<bool> B { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("76c6a4a8-a8e0-4e6e-b230-b4691a7c1e8c")]
        public IRuntimeConnector<bool> Y { get; internal set; }
    }
}
