using ViciOne.Core.Contracts.Runtime;

namespace Demo;

public partial class SettingsDemo
{
    protected override void Dispose(bool disposing)
    {
        if (Settings.DummySetting1 != null &&
            SetSettingValueIfNotEqual(Settings.DummySetting1, Settings.DummySetting1.Value == false))
            Logger.SettingValueChanged(FunctionBlock.Name, nameof(Settings.DummySetting1), Settings.DummySetting1.Value);

        base.Dispose(disposing);
    }

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (cycleInfo.CycleNo == 100)
            SetSettingValueIfNotEqual(Settings.DummySetting1, Settings.DummySetting1.Value == false);

        if (ProcessDataInputs.DummyForDefaultPooling2.Changed)
        {
            // TODO: All values accumulated so far from all input links are provided here
        }
    }
}
