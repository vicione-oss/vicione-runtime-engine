using ViciOne.Core.Contracts.Runtime;

namespace Numeric;

public partial class Add
{
    private void CalculateAndPublish()
        => SetConnectorValueIfNotEqual(ProcessDataOutputs.Sum, ProcessDataInputs.Addend1.Value + ProcessDataInputs.Addend2.Value);

    protected override void OnFunctionBlockEnabledChanged()
    {
        base.OnFunctionBlockEnabledChanged();
        if (FunctionBlockEnabled)
            CalculateAndPublish();
    }

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (ProcessDataInputs.Addend1.Changed || ProcessDataInputs.Addend2.Changed)
            CalculateAndPublish();
    }
}
