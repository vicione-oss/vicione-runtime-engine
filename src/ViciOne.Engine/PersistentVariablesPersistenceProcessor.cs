using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class PersistentVariablesPersistenceProcessor(IRuntimeFunctionBlock[] runtimeFunctionBlocks, IUniqueNameProvider uniqueNameProvider,
    IPersistence persistence, Func<IReferenceTypeCloneHandler> getCloneHandler, ILogger logger) :
    PersistenceProcessor<IRuntimePersistentVariables, IRuntimePersistentVariable>(persistence, getCloneHandler)
{
    protected override RuntimeElementsChangedObserver<IRuntimePersistentVariables, IRuntimePersistentVariable> ChangedObserver { get; } = new RuntimePersistentVariablesChangedObserver(runtimeFunctionBlocks);
    protected override RuntimeElementsProvider<IRuntimePersistentVariables, IRuntimePersistentVariable> ElementProvider { get; } = new RuntimePersistentVariablesProvider(runtimeFunctionBlocks);

    protected override void LogErrorWhileApplying(Exception ex, Guid loadedId, IIdProvider element)
        => logger.ApplyLoadedPersistentVariableValueErrorOccured(ex, loadedId, uniqueNameProvider.GetUniqueName(element));

    protected override void LogWarningVariableNotFound(Guid loadedId)
        => logger.NoPersistentVariableWithIdFound(loadedId);
}
