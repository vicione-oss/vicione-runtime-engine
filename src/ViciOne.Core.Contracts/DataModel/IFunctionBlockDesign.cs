using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IFunctionBlockDesign : IIdProvider
{
    uint DefaultCycleFrequency { get; }
    RunMode DefaultRunMode { get; }
    string Name { get; }
    string Namespace { get; }
    IEnumerable<IPersistentVariableDesign> PersistentVariables { get; }
    IEnumerable<IConnectorDesign> ProcessDataInputs { get; }
    IEnumerable<IConnectorDesign> ProcessDataOutputs { get; }
    string? RuntimeDriverTypeName { get; }
    string? RuntimeTypeName { get; }
    IEnumerable<ISettingDesign> Settings { get; }
    IEnumerable<IConnectorDesign> SystemInputs { get; }
    IEnumerable<IConnectorDesign> SystemOutputs { get; }
    IEnumerable<RunMode> ValidRunModes { get; }
}
