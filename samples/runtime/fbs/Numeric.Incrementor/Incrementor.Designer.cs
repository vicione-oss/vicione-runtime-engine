using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Engine.DefaultValidators;

namespace Numeric;

[FunctionBlockDesign("85dbbbd7-1a4e-4b82-94b8-1c34fbaaab60", "fff22efe-c07f-4153-98d0-60d83f42271b", "5e1db2db-caf0-4a3b-b862-d1c6fb8da391")]
public partial class Incrementor : RuntimeFunctionBlock<Incrementor>
{
    public new PersistentVariables_ PersistentVariables { get; } = new PersistentVariables_();
    [PersistentVariableDesigns]
    public class PersistentVariables_
    {
        [PersistentVariableDesign("1cc5f3c2-a5be-4ed7-a934-0ae2ac9e9d50")]
        public IRuntimePersistentVariable<double> Value { get; internal set; }
    }

    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("e14a86af-8d7d-4973-a5f4-e59d80ec2277")]
        public IRuntimeConnector<bool> Increment { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("b534ddee-d7c0-4ea4-9f2e-24dbfde8851b")]
        public IRuntimeConnector<double> Value { get; internal set; }
    }

    public new Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_
    {
        [SettingDesign("da73c7e1-98f3-44bc-bc52-65a9f30d336e", "c7923629-2854-4498-9f21-b3fd67bc4549", "02f6ebfa-5382-4fa2-b225-ba7aae310fc1")]
        [DefaultValue(0.5)]
        [InequalityValidator<double>(0.0, 0.5)]
        public IRuntimeSetting<double> IncrementValue { get; internal set; }
    }
}
