using ViciOne.Core.Contracts.Runtime;

namespace Demo;

public partial class SettingsDemo
{
    protected override void Dispose(bool disposing)
    {
        if (Settings.DummyEinstellung1 != null &&
            SetSettingValueIfNotEqual(Settings.DummyEinstellung1, Settings.DummyEinstellung1.Value == false))
            Logger.SettingValueChanged(FunctionBlock.Name, nameof(Settings.DummyEinstellung1), Settings.DummyEinstellung1.Value);

        base.Dispose(disposing);
    }

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (cycleInfo.CycleNo == 100)
            SetSettingValueIfNotEqual(Settings.DummyEinstellung1, Settings.DummyEinstellung1.Value == false);

        if (ProcessDataInputs.DummyForDefaultPooling2.Changed)
        {
            // TODO: Hier werden alle bisher aufgelaufenen Werte aller Eingangs-Links bereitgestellt
        }
    }
}
