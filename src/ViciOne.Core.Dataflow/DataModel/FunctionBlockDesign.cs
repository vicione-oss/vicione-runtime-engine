using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

[DebuggerDisplay("{Namespace,nq}.{Name,nq}")]
public class FunctionBlockDesign : IFunctionBlockDesign
{
    public uint DefaultCycleFrequency { get; set; }
    public RunMode DefaultRunMode { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<PersistentVariableDesign> PersistentVariables { get; set; } = [];
    IEnumerable<IPersistentVariableDesign> IFunctionBlockDesign.PersistentVariables => PersistentVariables;
    public List<ConnectorDesign> ProcessDataInputs { get; set; } = [];
    IEnumerable<IConnectorDesign> IFunctionBlockDesign.ProcessDataInputs => ProcessDataInputs;
    public List<ConnectorDesign> ProcessDataOutputs { get; set; } = [];
    IEnumerable<IConnectorDesign> IFunctionBlockDesign.ProcessDataOutputs => ProcessDataOutputs;
    public string? RuntimeDriverTypeName { get; set; }
    public string? RuntimeTypeName { get; set; }
    public List<SettingDesign> Settings { get; set; } = [];
    IEnumerable<ISettingDesign> IFunctionBlockDesign.Settings => Settings;
    public List<ConnectorDesign> SystemInputs { get; set; } = [];
    IEnumerable<IConnectorDesign> IFunctionBlockDesign.SystemInputs => SystemInputs;
    public List<ConnectorDesign> SystemOutputs { get; set; } = [];
    IEnumerable<IConnectorDesign> IFunctionBlockDesign.SystemOutputs => SystemOutputs;
    public List<RunMode> ValidRunModes { get; set; } = [];
    IEnumerable<RunMode> IFunctionBlockDesign.ValidRunModes => ValidRunModes;
}
