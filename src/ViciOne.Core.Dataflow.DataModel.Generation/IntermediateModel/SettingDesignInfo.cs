using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;

public sealed class SettingDesignInfo
{
    public object? DefaultValue { get; set; }
    public Guid Id { get; set; }
    public Guid InputId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OutputId { get; set; }
    public Type Type { get; set; } = typeof(object);
    public IValidator[] Validators { get; set; } = [];
}
