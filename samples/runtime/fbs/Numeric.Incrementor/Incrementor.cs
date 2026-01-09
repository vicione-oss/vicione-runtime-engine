using ViciOne.Core.Contracts.Runtime;

namespace Numeric;

public partial class Incrementor
{
    protected override void OnPersistentVariableChanged(IRuntimePersistentVariable variable)
       => SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, PersistentVariables.Value.Value);

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (cycleInfo.IsFirstCycle)
            SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, PersistentVariables.Value.Value);

        if (ProcessDataInputs.Increment.Changed && ProcessDataInputs.Increment.Value)
        {
            if (SetPersistentVariableIfNotEqual(PersistentVariables.Value, PersistentVariables.Value.Value + Settings.IncrementValue.Value))
                SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, PersistentVariables.Value.Value);
        }
    }
}
