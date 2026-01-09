using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeFunctionBlock : IDisposable
{
    uint CycleFrequency { get; }
    IFunctionBlock FunctionBlock { get; }
    IRuntimePersistentVariables PersistentVariables { get; }
    IEnumerable<IRuntimeConnector> ProcessDataInputs { get; }
    IEnumerable<IRuntimeConnector> ProcessDataOutputs { get; }
    RunMode RunMode { get; }
    IRuntimeSettings Settings { get; }
    IEnumerable<IRuntimeConnector> SettingsInputs { get; }
    IEnumerable<IRuntimeConnector> SettingsOutputs { get; }
    IEnumerable<IRuntimeConnector> SystemInputs { get; }
    IEnumerable<IRuntimeConnector> SystemOutputs { get; }

    bool GetAndResetChanged();
    void InitializeI();
    void InitializeII();
    void MarkAsChanged();
    void ProcessCycle(IFunctionBlockCycleInfo cycleInfo);
    void Setup(IFunctionBlock functionBlock, IRuntimeDriver? driver, HashSet<Guid> connectedConnectors, IUniqueNameProvider uniqueNameProvider, ILogger logger);
}
