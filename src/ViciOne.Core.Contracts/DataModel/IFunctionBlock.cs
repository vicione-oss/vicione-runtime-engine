using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IFunctionBlock : IIdProvider
{
    uint CycleFrequency { get; }
    IFunctionBlockDesign Design { get; }
    string Name { get; }
    IEnumerable<IPersistentVariable> PersistentVariables { get; }
    IEnumerable<IConnector> ProcessDataInputs { get; }
    IEnumerable<IConnector> ProcessDataOutputs { get; }
    RunMode RunMode { get; }
    IEnumerable<ISetting> Settings { get; }
    IEnumerable<IConnector> SettingsInputs { get; }
    IEnumerable<IConnector> SettingsOutputs { get; }
    IEnumerable<IConnector> SystemInputs { get; }
    IEnumerable<IConnector> SystemOutputs { get; }
}
