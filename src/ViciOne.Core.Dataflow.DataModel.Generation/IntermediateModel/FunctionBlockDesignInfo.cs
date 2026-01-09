using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;

public sealed class FunctionBlockDesignInfo
{
    public uint? DefaultCycleFrequency { get; set; }
    public RunMode? DefaultRunMode { get; set; }
    public Guid FunctionBlockEnabledInputId { get; set; }
    public Guid FunctionBlockEnabledOutputId { get; set; }
    public Guid Id { get; set; }
    public List<ConnectorDesignInfo> Inputs { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<ConnectorDesignInfo> Outputs { get; set; } = [];
    public List<PersistentVariableDesignInfo> PersistentVariables { get; set; } = [];
    public string? RuntimeDriverTypeName { get; set; }
    public string? RuntimeTypeName { get; set; }
    public List<SettingDesignInfo> Settings { get; set; } = [];
    public RunMode[]? ValidRunModes { get; set; }
}
