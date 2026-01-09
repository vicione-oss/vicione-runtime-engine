using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Dataflow.Pooling;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class FunctionBlockDesignEditor
{
    private readonly Dictionary<ConnectorDesign, IEnumerable<ConnectorDesign>> _connectorDesignListMap;

    public virtual FunctionBlockDesign Design { get; }

    public FunctionBlockDesignEditor(PoolingFactory poolingFactory) : this(CreateModel(poolingFactory)) { }

    public FunctionBlockDesignEditor(FunctionBlockDesign design)
    {
        Design = design;
        _connectorDesignListMap = CreateConnectorDesignListMap(Design);
    }

    // TODO: Es wird nur noch in der Liste, in die das Design gelegt wird, auf Eindeutigkeit geprüft. Das sollte doch reichen, oder?
    protected virtual void AddConnectorDesign(List<ConnectorDesign> designs, IConnectorDesignEditor editor)
    {
        if (designs.Contains(editor.Design))
            return;
        ValidateConnectorDesignNotAdded(editor);
        ValidateConnectorDesignNameNotExisting(designs, editor.Design.Name);
        designs.Add(editor.Design);
        _connectorDesignListMap.Add(editor.Design, designs);
        editor.FunctionBlockDesignEditor = this;
    }

    public virtual void AddPersistentVariable(IPersistentVariableDesignEditor editor)
    {
        ValidatePersistentVariableNotAdded(editor);
        ValidatePersistentVariableDesignNameNotExisting(editor.PersistentVariable.Name);
        editor.FunctionBlockDesignEditor = this;
        Design.PersistentVariables.Add(editor.PersistentVariable);
    }

    public virtual void AddProcessDataInput(IConnectorDesignEditor editor)
        => AddConnectorDesign(Design.ProcessDataInputs, editor);

    public virtual void AddProcessDataOutput(IConnectorDesignEditor editor)
        => AddConnectorDesign(Design.ProcessDataOutputs, editor);

    public virtual void AddSetting(ISettingDesignEditor editor)
    {
        ValidateSettingNotAdded(editor);
        ValidateSettingDesignNameNotExisting(editor.Setting.Name);
        editor.FunctionBlockDesignEditor = this;
        Design.Settings.Add(editor.Setting);
    }

    public virtual void AddValidRunMode(RunMode mode)
    {
        if (Design.ValidRunModes.Contains(mode) == false)
            Design.ValidRunModes.Add(mode);
    }

    private static Dictionary<ConnectorDesign, IEnumerable<ConnectorDesign>> CreateConnectorDesignListMap(FunctionBlockDesign design)
    {
        var map = new Dictionary<ConnectorDesign, IEnumerable<ConnectorDesign>>();
        AddToMap(design.SystemInputs);
        AddToMap(design.SystemOutputs);
        AddToMap(design.ProcessDataInputs);
        AddToMap(design.ProcessDataOutputs);
        return map;

        void AddToMap(List<ConnectorDesign> listToAdd)
            => listToAdd.ForEach(connectorDesign => map[connectorDesign] = listToAdd);
    }

    private static FunctionBlockDesign CreateModel(PoolingFactory poolingFactory)
    {
        ConnectorDesignInput<bool> fbEnabledInputDesign = new ConnectorDesignInputEditor<bool>("dummy", poolingFactory).Design;
        fbEnabledInputDesign.DefaultValue = true;
        fbEnabledInputDesign.Name = SystemConnectorNames.FunctionBlockEnabledName;

        ConnectorDesignOutput<bool> fbEnabledOutputDesign = new ConnectorDesignOutputEditor<bool>("dummy").Design;
        fbEnabledOutputDesign.DefaultValue = false;
        fbEnabledOutputDesign.Name = SystemConnectorNames.FunctionBlockEnabledName;

        var fbDesign = new FunctionBlockDesign
        {
            Id = Guid.NewGuid(),
            ValidRunModes = [.. FunctionBlockDesignDefaults.ValidRunModes],
            DefaultCycleFrequency = FunctionBlockDesignDefaults.CycleFrequency,
            DefaultRunMode = FunctionBlockDesignDefaults.RunMode,
            SystemInputs = [fbEnabledInputDesign],
            SystemOutputs = [fbEnabledOutputDesign]
        };

        return fbDesign;
    }

    protected virtual bool IsConnectorDesignNameExisting(string name)
        => Design.GetConnectors().Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    protected virtual bool RemoveConnectorDesign(List<ConnectorDesign> designs, IConnectorDesignEditor editor)
    {
        bool returnValue = designs.Remove(editor.Design);
        _connectorDesignListMap.Remove(editor.Design);
        editor.FunctionBlockDesignEditor = null;
        return returnValue;
    }

    public virtual bool RemovePersistentVariable(IPersistentVariableDesignEditor editor)
    {
        bool returnValue = Design.PersistentVariables.Remove(editor.PersistentVariable);
        editor.FunctionBlockDesignEditor = null;
        return returnValue;
    }

    public virtual bool RemoveProcessDataInput(IConnectorDesignEditor editor)
        => RemoveConnectorDesign(Design.ProcessDataInputs, editor);

    public virtual bool RemoveProcessDataOutput(IConnectorDesignEditor editor)
        => RemoveConnectorDesign(Design.ProcessDataOutputs, editor);

    public virtual bool RemoveSetting(ISettingDesignEditor editor)
    {
        bool returnValue = Design.Settings.Remove(editor.Setting);
        editor.FunctionBlockDesignEditor = null;
        return returnValue;
    }

    public virtual bool RemoveValidRunMode(RunMode mode)
    {
        if (Design.ValidRunModes.Contains(mode) == false)
            return false;
        ValidateRunModeToBeRemovedIsNotDefaultRunMode(mode);
        ValidateValidRunModesCountGreaterThanOneBeforeRemove();
        return Design.ValidRunModes.Remove(mode);
    }

    public virtual void SetDefaultCycleFrequency(uint cycleFrequency)
        => Design.DefaultCycleFrequency = cycleFrequency;

    public virtual void SetDefaultRunMode(RunMode mode)
        => Design.DefaultRunMode = ValidateDefaultRunMode(mode);

    public virtual void SetName(string name)
        => Design.Name = ValidateNameOrNamespace(name);

    public virtual void SetNamespace(string @namespace)
        => Design.Namespace = ValidateNameOrNamespace(@namespace);

    public virtual void SetRuntimeDriverTypeName(string? driverTypeName)
        => Design.RuntimeDriverTypeName = ValidateTypeName(driverTypeName);

    public virtual void SetRuntimeTypeName(string? typeName)
        => Design.RuntimeTypeName = ValidateTypeName(typeName);

    internal virtual void ValidateConnectorDesignNameNotExisting(ConnectorDesign design, string name)
    {
        if (_connectorDesignListMap.TryGetValue(design, out IEnumerable<ConnectorDesign>? designs))
            ValidateConnectorDesignNameNotExisting(designs, name);
    }

    protected virtual void ValidateConnectorDesignNameNotExisting(IEnumerable<ConnectorDesign> designs, string name)
    {
        if (designs.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A connector with this name is already existing.");
    }

    protected virtual void ValidateConnectorDesignNotAdded(IConnectorDesignEditor editor)
    {
        if (editor.FunctionBlockDesignEditor is not null)
            throw new InvalidOperationException($"This {nameof(ConnectorDesign)} was already added to a {nameof(FunctionBlockDesign)}.");
    }

    protected virtual RunMode ValidateDefaultRunMode(RunMode mode) => Design.ValidRunModes.Contains(mode) == false
        ? throw new ArgumentException($"The {nameof(RunMode)} '{mode}' is not valid.{Environment.NewLine}" +
                                      $"Valid {nameof(RunMode)}s are:{Environment.NewLine}{Environment.NewLine}" +
                                      $"{string.Join(Environment.NewLine, Design.ValidRunModes)}")
        : mode;

    protected virtual string? ValidateTypeName(string? typeName)
        => typeName is null ? null : ValidateNameOrNamespace(typeName);

    protected virtual string ValidateNameOrNamespace(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException($"{nameof(identifier)} cannot be null or empty.", nameof(identifier));

        identifier = identifier.Trim();
        return NameValidator.IsValidNamespaceOrClassName(identifier) == false
            ? throw ExceptionFactory.CreateNoValidNamespaceOrClassNameException(identifier)
            : identifier;
    }

    internal virtual void ValidatePersistentVariableDesignNameNotExisting(string name)
    {
        if (Design.PersistentVariables.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A persistent variable with this name is already existing.");
    }

    protected virtual void ValidatePersistentVariableNotAdded(IPersistentVariableDesignEditor editor)
    {
        if (editor.FunctionBlockDesignEditor is not null)
            throw new InvalidOperationException($"This {nameof(PersistentVariableDesign)} was already added to a {nameof(FunctionBlockDesignEditor)}.");
    }

    protected virtual void ValidateRunModeToBeRemovedIsNotDefaultRunMode(RunMode mode)
    {
        if (Design.DefaultRunMode == mode)
            throw new InvalidOperationException($"The {nameof(RunMode)} '{mode}' cannot be removed because it is set as default {nameof(RunMode)}.");
    }

    internal virtual void ValidateSettingDesignNameNotExisting(string name)
    {
        if (Design.Settings.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A setting with this name is already existing.");

        ValidateConnectorDesignNameNotExisting(Design.ProcessDataInputs, name);
        ValidateConnectorDesignNameNotExisting(Design.ProcessDataOutputs, name);
    }

    protected virtual void ValidateSettingNotAdded(ISettingDesignEditor editor)
    {
        if (editor.FunctionBlockDesignEditor is not null)
            throw new InvalidOperationException($"This {nameof(SettingDesign)} was already added to a {nameof(FunctionBlockDesignEditor)}.");
    }

    protected virtual void ValidateValidRunModesCountGreaterThanOneBeforeRemove()
    {
        if (Design.ValidRunModes.Count <= 1)
            throw new InvalidOperationException($"At least one {nameof(RunMode)} must remain in the list.");
    }
}
