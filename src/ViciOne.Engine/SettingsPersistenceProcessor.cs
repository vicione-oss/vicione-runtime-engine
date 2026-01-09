using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class SettingsPersistenceProcessor(IRuntimeFunctionBlock[] runtimeFunctionBlocks, IUniqueNameProvider uniqueNameProvider,
    IPersistence persistence, Func<IReferenceTypeCloneHandler> getCloneHandler, ILogger logger) :
    PersistenceProcessor<IRuntimeSettings, IRuntimeSetting>(persistence, getCloneHandler)
{
    private readonly ILogger _logger = logger;
    private readonly IUniqueNameProvider _uniqueNameProvider = uniqueNameProvider;

    protected override RuntimeElementsChangedObserver<IRuntimeSettings, IRuntimeSetting> ChangedObserver { get; } = new RuntimeSettingsChangedObserver(runtimeFunctionBlocks);
    protected override RuntimeElementsProvider<IRuntimeSettings, IRuntimeSetting> ElementProvider { get; } = new RuntimeSettingsProvider(runtimeFunctionBlocks);

    protected override void LogErrorWhileApplying(Exception ex, Guid loadedId, IIdProvider element)
        => _logger.ApplyLoadedSettingValueErrorOccured(ex, loadedId, _uniqueNameProvider.GetUniqueName(element));

    protected override void LogWarningVariableNotFound(Guid loadedId)
        => _logger.NoSettingWithIdFound(loadedId);
}
