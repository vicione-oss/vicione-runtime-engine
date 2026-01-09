using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class PersistentVariablesPersistenceProcessor(IRuntimeFunctionBlock[] runtimeFunctionBlocks, IUniqueNameProvider uniqueNameProvider,
    IPersistence persistence, Func<IReferenceTypeCloneHandler> getCloneHandler, ILogger logger) :
    PersistenceProcessor<IRuntimePersistentVariables, IRuntimePersistentVariable>(persistence, getCloneHandler)
{
    private readonly ILogger _logger = logger;
    private readonly IUniqueNameProvider _uniqueNameProvider = uniqueNameProvider;

    protected override RuntimeElementsChangedObserver<IRuntimePersistentVariables, IRuntimePersistentVariable> ChangedObserver { get; } = new RuntimePersistentVariablesChangedObserver(runtimeFunctionBlocks);
    protected override RuntimeElementsProvider<IRuntimePersistentVariables, IRuntimePersistentVariable> ElementProvider { get; } = new RuntimePersistentVariablesProvider(runtimeFunctionBlocks);

    protected override void LogErrorWhileApplying(Exception ex, Guid loadedId, IIdProvider element)
        => _logger.ApplyLoadedPersistentVariableValueErrorOccured(ex, loadedId, _uniqueNameProvider.GetUniqueName(element));

    protected override void LogWarningVariableNotFound(Guid loadedId)
        => _logger.NoPersistentVariableWithIdFound(loadedId);
}
