using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Stubs;

internal sealed class EmptyFunctionBlock : IFunctionBlock
{
    public uint CycleFrequency => default;
    public IFunctionBlockDesign Design => Throw<IFunctionBlockDesign>();
    public string Name => string.Empty;
    public IEnumerable<IPersistentVariable> PersistentVariables => Throw<IEnumerable<IPersistentVariable>>();
    public IEnumerable<IConnector> ProcessDataInputs => Throw<IEnumerable<IConnector>>();
    public IEnumerable<IConnector> ProcessDataOutputs => Throw<IEnumerable<IConnector>>();
    public RunMode RunMode => default;
    public IEnumerable<ISetting> Settings => Throw<IEnumerable<ISetting>>();
    public IEnumerable<IConnector> SettingsInputs => Throw<IEnumerable<IConnector>>();
    public IEnumerable<IConnector> SettingsOutputs => Throw<IEnumerable<IConnector>>();
    public IEnumerable<IConnector> SystemInputs => Throw<IEnumerable<IConnector>>();
    public IEnumerable<IConnector> SystemOutputs => Throw<IEnumerable<IConnector>>();
    public Guid Id => default;

    private static T Throw<T>()
        => throw new InvalidOperationException("This function block is only for design purposes and not intended to be used at runtime.");
}
