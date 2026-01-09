using ViciOne.Core.Contracts.Runtime;

public abstract class RuntimeSettingsTests : RuntimePeristableElementsTests
{
    private protected static IRuntimeFunctionBlock CreateBlockWithSettings(int settingsCount)
        => MockBlockWithEmptyElements<IRuntimeSettings, IRuntimeSetting>(settingsCount, (block) => block.Settings);

    private protected static DisposableList<IRuntimeFunctionBlock> CreateBlocksWithSettings(params int[] settingsCounts)
    {
        DisposableList<IRuntimeFunctionBlock> blocks = [];
        foreach (var settingsCount in settingsCounts)
            blocks.Add(CreateBlockWithSettings(settingsCount));
        return blocks;
    }
}
