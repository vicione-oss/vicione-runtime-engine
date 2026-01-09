using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

[DebuggerDisplay("{Design.Namespace,nq}.{Design.Name,nq}.{Name,nq}")]
public class FunctionBlock : IFunctionBlock
{
    public uint CycleFrequency { get; set; }
    public FunctionBlockDesign Design { get; set; } = new FunctionBlockDesign();
    IFunctionBlockDesign IFunctionBlock.Design => Design;
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PersistentVariable> PersistentVariables { get; set; } = [];
    IEnumerable<IPersistentVariable> IFunctionBlock.PersistentVariables => PersistentVariables;
    public List<Connector> ProcessDataInputs { get; set; } = [];
    IEnumerable<IConnector> IFunctionBlock.ProcessDataInputs => ProcessDataInputs;
    public List<Connector> ProcessDataOutputs { get; set; } = [];
    IEnumerable<IConnector> IFunctionBlock.ProcessDataOutputs => ProcessDataOutputs;
    public RunMode RunMode { get; set; }
    public List<Setting> Settings { get; set; } = [];
    IEnumerable<ISetting> IFunctionBlock.Settings => Settings;
    public IEnumerable<Connector> SettingsInputs => Settings.Select(x => x.Input);
    IEnumerable<IConnector> IFunctionBlock.SettingsInputs => SettingsInputs;
    public IEnumerable<Connector> SettingsOutputs => Settings.Select(x => x.Output);
    IEnumerable<IConnector> IFunctionBlock.SettingsOutputs => SettingsOutputs;
    public List<Connector> SystemInputs { get; set; } = [];
    IEnumerable<IConnector> IFunctionBlock.SystemInputs => SystemInputs;
    public List<Connector> SystemOutputs { get; set; } = [];
    IEnumerable<IConnector> IFunctionBlock.SystemOutputs => SystemOutputs;
}
