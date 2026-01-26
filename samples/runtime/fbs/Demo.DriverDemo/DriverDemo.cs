using System;
using ViciOne.Core.Contracts.Runtime;

namespace Demo;

public partial class DriverDemo
{
    private bool _disposed;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_disposed)
            return;
        _disposed = true;
        Driver.TimeChanged -= Driver_TimeChanged;
    }

    private void Driver_TimeChanged(object? sender, EventArgs e)
        => PublishTime();

    public override void InitializeI()
    {
        base.InitializeI();
        Driver.Machwas(FunctionBlock.Name);
        Driver.TimeChanged += Driver_TimeChanged;
    }

    protected override void OnFunctionBlockEnabledChanged()
    {
        base.OnFunctionBlockEnabledChanged();
        PublishTime();
    }

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (cycleInfo.IsFirstCycle)
            PublishTime();
    }

    private void PublishTime()
    {
        if (FunctionBlockEnabled && SetConnectorValueIfNotEqual(ProcessDataOutputs.Time, Driver.Time))
            SetConnectorValueIfNotEqual(ProcessDataOutputs.Toggle, ProcessDataOutputs.Toggle.Value == false);
    }
}
