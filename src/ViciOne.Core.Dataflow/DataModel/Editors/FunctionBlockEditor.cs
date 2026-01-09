using System;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class FunctionBlockEditor(FunctionBlock functionBlock)
{
    public virtual ContainerEditor? ContainerEditor { get; protected internal set; }
    public virtual FunctionBlock FunctionBlock { get; } = functionBlock;

    public FunctionBlockEditor(FunctionBlockDesign design) : this(CreateModel(design)) { }

    private static FunctionBlock CreateModel(FunctionBlockDesign design)
        => new()
        {
            Design = design,
            Id = Guid.NewGuid(),
            Name = ValidateNameBase(design.Name),
            CycleFrequency = design.DefaultCycleFrequency,
            RunMode = design.DefaultRunMode,
            PersistentVariables = [.. design.PersistentVariables.Select(PersistentVariableFactory.DesignToPersistentVariable)],
            Settings = [.. design.Settings.Select(SettingFactory.DesignToSetting)],
            SystemInputs = [.. design.SystemInputs.Select(ConnectorFactory.DesignToConnector<Connector>)],
            SystemOutputs = [.. design.SystemOutputs.Select(ConnectorFactory.DesignToConnector<Connector>)],
            ProcessDataInputs = [.. design.ProcessDataInputs.Select(ConnectorFactory.DesignToConnector<Connector>)],
            ProcessDataOutputs = [.. design.ProcessDataOutputs.Select(ConnectorFactory.DesignToConnector<Connector>)]
        };

    public virtual void ResetToDefaults()
    {
        SetCycleFrequency(FunctionBlock.Design.DefaultCycleFrequency);
        SetRunMode(FunctionBlock.Design.DefaultRunMode);
    }

    public virtual void SetCycleFrequency(uint cycleFrequency)
        => FunctionBlock.CycleFrequency = cycleFrequency;

    public virtual void SetName(string name)
       => FunctionBlock.Name = ValidateName(name);

    public virtual void SetRunMode(RunMode mode)
    {
        if (FunctionBlock.Design.ValidRunModes.Contains(mode) == false)
            throw new InvalidOperationException($"{nameof(RunMode)} '{mode}' is not valid.");

        FunctionBlock.RunMode = mode;
    }

    protected virtual string ValidateName(string name)
    {
        name = ValidateNameBase(name);

        if (name.Equals(FunctionBlock.Name, StringComparison.Ordinal) == false)
            ValidateNameNotExisting(name);

        return name;
    }

    protected static string ValidateNameBase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));

        return name.Trim();
    }

    protected virtual void ValidateNameNotExisting(string name)
        => ContainerEditor?.ValidateFunctionBlockOrContainerNameNotExisting(name);
}
