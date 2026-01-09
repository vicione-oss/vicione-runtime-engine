using ViciOne.Core.Contracts.Runtime;

namespace Logic;

public partial class AND
{
    private void CalculateAndPublish()
        => SetConnectorValueIfNotEqual(ProcessDataOutputs.Y, ProcessDataInputs.A.Value & ProcessDataInputs.B.Value);

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

        if (ProcessDataInputs.A.Changed || ProcessDataInputs.B.Changed)
            CalculateAndPublish();
    }
}
