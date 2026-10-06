using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine.Benchmarks;

internal sealed class StubRuntimeFunctionBlock(IEnumerable<IRuntimeConnector> processDataInputs) : IRuntimeFunctionBlock
{
    public uint CycleFrequency => throw new NotSupportedException();
    public IFunctionBlock FunctionBlock => throw new NotSupportedException();
    public IRuntimePersistentVariables PersistentVariables => throw new NotSupportedException();
    public IEnumerable<IRuntimeConnector> ProcessDataInputs { get; } = processDataInputs;
    public IEnumerable<IRuntimeConnector> ProcessDataOutputs => [];
    public RunMode RunMode => throw new NotSupportedException();
    public IRuntimeSettings Settings => throw new NotSupportedException();
    public IEnumerable<IRuntimeConnector> SettingsInputs => [];
    public IEnumerable<IRuntimeConnector> SettingsOutputs => [];
    public IEnumerable<IRuntimeConnector> SystemInputs => [];
    public IEnumerable<IRuntimeConnector> SystemOutputs => [];

    public void Dispose() { }

    public bool GetAndResetChanged()
        => throw new NotSupportedException();

    public void InitializeI()
        => throw new NotSupportedException();

    public void InitializeII()
        => throw new NotSupportedException();

    public void MarkAsChanged()
        => throw new NotSupportedException();

    public void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
        => throw new NotSupportedException();

    public void Setup(IFunctionBlock functionBlock, IRuntimeDriver? driver, HashSet<Guid> connectedConnectors, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => throw new NotSupportedException();
}
