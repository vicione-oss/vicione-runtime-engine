using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;

namespace Demo;

[FunctionBlockDesign("95403b29-3c37-4c59-9c73-acc6f26358cf", "c0e9efe9-261b-4098-9bcc-7db2b55ed025", "390958b7-d2eb-41d1-9d1c-91de43c1ba1a")]
public partial class ComplexTypeDemo : RuntimeFunctionBlock<ComplexTypeDemo>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("eb8b9049-ec2c-407c-849a-3b1065b054cf")]
        [DefaultValue<Derived>]
        public IRuntimeConnector<Base> In { get; internal set; }
        [InputConnectorDesign("2a54a9a4-e558-409f-a5e7-ba00b7eb29d5")]
        public IRuntimeConnector<bool> GenerateBase { get; internal set; }
        [InputConnectorDesign("8872c3e4-231c-4ba4-bb29-bec9bfbe361b")]
        public IRuntimeConnector<bool> GenerateDerived { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("92adec66-917a-43ab-ae43-d170f9928e81")]
        public IRuntimeConnector<Base> GeneratedBase { get; internal set; }
        [OutputConnectorDesign("0d0e6918-b566-4546-929e-d09c4a32a62c")]
        public IRuntimeConnector<Derived> GeneratedDerived { get; internal set; }
        [OutputConnectorDesign("f58206dc-2e2b-4229-974c-31bd420e7a01")]
        public IRuntimeConnector<bool> InIsNotNull { get; internal set; }
        [OutputConnectorDesign("036396ce-350c-4242-9709-1af3777d66b6")]
        public IRuntimeConnector<int> InNumericValue { get; internal set; }
        [OutputConnectorDesign("bf0c1827-c442-43f7-9b91-e8df86cb927a")]
        public IRuntimeConnector<bool> InIsDerived { get; internal set; }
        [OutputConnectorDesign("d057774b-c11d-4617-92dd-9225deea7cd8")]
        public IRuntimeConnector<string> InStringValue { get; internal set; }
    }

    public new Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_
    {
        [SettingDesign("935824ad-d082-4140-a4a2-bb254c3d4508", "e1d92114-20ed-4340-90c8-37e306c090bf", "e5d58cc3-7fe8-4e78-bba8-d454f8b7b290")]
        [DefaultValue(42)]
        public IRuntimeSetting<int> NumericValueOffset { get; internal set; }
    }
}
