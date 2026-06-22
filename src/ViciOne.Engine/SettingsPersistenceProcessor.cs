using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class SettingsPersistenceProcessor(IRuntimeFunctionBlock[] runtimeFunctionBlocks, IUniqueNameProvider uniqueNameProvider,
    IPersistence persistence, Func<IReferenceTypeCloneHandler> getCloneHandler, ILogger logger) :
    PersistenceProcessor<IRuntimeSettings, IRuntimeSetting>(persistence, getCloneHandler)
{
    protected override RuntimeElementsChangedObserver<IRuntimeSettings, IRuntimeSetting> ChangedObserver { get; } = new RuntimeSettingsChangedObserver(runtimeFunctionBlocks);
    protected override RuntimeElementsProvider<IRuntimeSettings, IRuntimeSetting> ElementProvider { get; } = new RuntimeSettingsProvider(runtimeFunctionBlocks);

    protected override void LogErrorWhileApplying(Exception ex, Guid loadedId, IIdProvider element)
        => logger.ApplyLoadedSettingValueErrorOccured(ex, loadedId, uniqueNameProvider.GetUniqueName(element));

    protected override void LogWarningVariableNotFound(Guid loadedId)
        => logger.NoSettingWithIdFound(loadedId);
}
