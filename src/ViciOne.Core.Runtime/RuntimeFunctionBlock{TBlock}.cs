using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Core.Runtime.Stubs;

namespace ViciOne.Core.Runtime;

public abstract class RuntimeFunctionBlock<TBlock> : IRuntimeFunctionBlock where TBlock : RuntimeFunctionBlock<TBlock>, new()
{
    private int _changed = 1;
    private HashSet<Guid>? _connectedConnectors;
    private bool _disposed;
    private IRuntimeConnector<bool> _functionBlockEnabledInput = new EmptyRuntimeConnector<bool>();
    private IRuntimeConnector<bool> _functionBlockEnabledOutput = new EmptyRuntimeConnector<bool>();
    private bool _setupCalled;
    private IUniqueNameProvider _uniqueNameProvider = new EmptyUniqueNameProvider();

    protected bool Changed => _changed == 1;
    public uint CycleFrequency { get; set; }
    public IRuntimeDriver? Driver { get; private set; }
    public IFunctionBlock FunctionBlock { get; private set; } = new EmptyFunctionBlock();
    public bool FunctionBlockEnabled => _functionBlockEnabledInput.Value;
    protected ILogger Logger { get; private set; } = NullLogger.Instance;
    public IRuntimePersistentVariables PersistentVariables { get; } = new RuntimePersistentVariables();
    protected IList<IRuntimeConnector> ProcessDataInputs { get; } = [];
    IEnumerable<IRuntimeConnector> IRuntimeFunctionBlock.ProcessDataInputs => ProcessDataInputs;
    protected IList<IRuntimeConnector> ProcessDataOutputs { get; } = [];
    IEnumerable<IRuntimeConnector> IRuntimeFunctionBlock.ProcessDataOutputs => ProcessDataOutputs;
    public RunMode RunMode { get; set; }
    public IRuntimeSettings Settings { get; } = new RuntimeSettings();
    public IEnumerable<IRuntimeConnector> SettingsInputs => Settings.Select(x => x.Input);
    public IEnumerable<IRuntimeConnector> SettingsOutputs => Settings.Select(x => x.Output);
    protected IList<IRuntimeConnector> SystemInputs { get; } = [];
    IEnumerable<IRuntimeConnector> IRuntimeFunctionBlock.SystemInputs => SystemInputs;
    protected IList<IRuntimeConnector> SystemOutputs { get; } = [];
    IEnumerable<IRuntimeConnector> IRuntimeFunctionBlock.SystemOutputs => SystemOutputs;

    private void ClearConnectedConnectors()
    {
        _connectedConnectors?.Clear();
        _connectedConnectors = null;
    }

    protected IRuntimePersistentVariable<TValue> CreatePersistentVariable<TValue>(string name)
        => CreateRuntimePersistentVariable<TValue>(name);

    protected IRuntimeConnector<TValue> CreateProcessDataInputConnector<TValue>(string name)
        => CreateRuntimeConnector<TValue>(name, FunctionBlock.ProcessDataInputs, "input");

    protected IRuntimeConnector<TValue> CreateProcessDataOutputConnector<TValue>(string name)
        => CreateRuntimeConnector<TValue>(name, FunctionBlock.ProcessDataOutputs, "output");

    private RuntimeConnector<TValue> CreateRuntimeConnector<TValue>(string name, IEnumerable<IConnector> connectorList, string connectorTypeForErrorMessage)
    {
        IConnector<TValue> connector = GetConnector(name, connectorList) as IConnector<TValue>
            ?? throw new ArgumentException($"Specified {connectorTypeForErrorMessage} connector was not found.", nameof(name));
        return new RuntimeConnector<TValue>(connector, _connectedConnectors!.Contains(connector.Id));
    }

    private RuntimePersistentVariable<TValue> CreateRuntimePersistentVariable<TValue>(string name)
    {
        IPersistentVariable<TValue> variable = GetPersistentVariable(name) as IPersistentVariable<TValue>
            ?? throw new ArgumentException($"Specified persistent variable was not found.", nameof(name));
        return new RuntimePersistentVariable<TValue>(variable);
    }

    private RuntimeSetting<TValue> CreateRuntimeSetting<TValue>(string name)
    {
        ISetting<TValue> setting = GetSetting(name) as ISetting<TValue>
            ?? throw new ArgumentException($"Specified setting was not found.", nameof(name));
        return new RuntimeSetting<TValue>(setting,
            _connectedConnectors!.Contains(setting.Input.Id),
            _connectedConnectors!.Contains(setting.Output.Id));
    }

    protected IRuntimeSetting<TValue> CreateSetting<TValue>(string name)
        => CreateRuntimeSetting<TValue>(name);

    protected IRuntimeConnector<TValue> CreateSystemInputConnector<TValue>(string name)
        => CreateRuntimeConnector<TValue>(name, FunctionBlock.SystemInputs, "system input");

    protected IRuntimeConnector<TValue> CreateSystemOutputConnector<TValue>(string name)
        => CreateRuntimeConnector<TValue>(name, FunctionBlock.SystemOutputs, "system output");

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            ClearConnectedConnectors();

            PersistentVariables.VariableChanged -= OnPersistentVariableChanged;
            PersistentVariables.Dispose();

            Settings.Changed -= OnSettingsChanged;
            Settings.SettingChanged -= OnSettingChanged;
            Settings.Dispose();
        }

        _disposed = true;
    }

    public bool GetAndResetChanged()
        => Interlocked.Exchange(ref _changed, 0) == 1;

    private static IConnector GetConnector(string name, IEnumerable<IConnector> list)
        => list.FirstOrDefault(x => string.Equals(x.Design.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Specified connector was not found.", nameof(name));

    private IPersistentVariable GetPersistentVariable(string name)
       => FunctionBlock.PersistentVariables.FirstOrDefault(x => string.Equals(x.Design.Name, name, StringComparison.OrdinalIgnoreCase))
           ?? throw new ArgumentException("Specified persistent variable was not found.", nameof(name));

    private ISetting GetSetting(string name)
        => FunctionBlock.Settings.FirstOrDefault(x => string.Equals(x.Design.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Specified setting was not found.", nameof(name));

    protected string GetUniqueName(IIdProvider idProvider)
        => _uniqueNameProvider.GetUniqueName(idProvider);

    private void InitializeComponents(Type classAttributeType, Type propertyAttributeType, MethodInfo createInstanceMethodDefinition, Action<object> addToList)
    {
        Type[] nestedTypes = GetType().GetNestedTypes();
        Type? classType = nestedTypes.FirstOrDefault(x => Attribute.IsDefined(x, classAttributeType));
        if (classType is null)
            return;

        PropertyInfo? classProperty = GetType().GetProperties().FirstOrDefault(x => x.PropertyType == classType);
        if (classProperty is null)
            return;

        object? classInstance = classProperty.GetValue(this);
        IEnumerable<PropertyInfo> entries = classType.GetProperties().Where(x => Attribute.IsDefined(x, propertyAttributeType));
        foreach (PropertyInfo entry in entries)
        {
            Type valueType = DataTypeCompatibilityValidator.DetermineValueType(entry.PropertyType);
            MethodInfo createMethod = createInstanceMethodDefinition.MakeGenericMethod(valueType);
            object entryInstance = createMethod.Invoke(this, [entry.Name])!;
            entry.SetValue(classInstance, entryInstance);
            addToList(entryInstance);
        }
    }

    protected virtual void InitializeConnectors()
    {
        InitializeComponents(typeof(InputConnectorDesignsAttribute), typeof(InputConnectorDesignAttribute),
            GetType().GetMethod(nameof(CreateProcessDataInputConnector), BindingFlags.NonPublic | BindingFlags.Instance)!,
            (x) => ProcessDataInputs.Add((IRuntimeConnector)x));

        InitializeComponents(typeof(OutputConnectorDesignsAttribute), typeof(OutputConnectorDesignAttribute),
            GetType().GetMethod(nameof(CreateProcessDataOutputConnector), BindingFlags.NonPublic | BindingFlags.Instance)!,
            (x) => ProcessDataOutputs.Add((IRuntimeConnector)x));
    }

    public virtual void InitializeI()
    {
        InitializeSettings();
        InitializePersistentVariables();
        InitializeConnectors();
        ClearConnectedConnectors();

        ValidateSettings();
        ValidatePersistentVariables();
        ValidateConnectors();
    }

    public virtual void InitializeII() { }

    protected virtual void InitializePersistentVariables()
        => InitializeComponents(typeof(PersistentVariableDesignsAttribute), typeof(PersistentVariableDesignAttribute),
            GetType().GetMethod(nameof(CreatePersistentVariable), BindingFlags.NonPublic | BindingFlags.Instance)!,
            (x) => PersistentVariables.Add((IRuntimePersistentVariable)x));

    protected virtual void InitializeSettings()
        => InitializeComponents(typeof(SettingDesignsAttribute), typeof(SettingDesignAttribute),
            GetType().GetMethod(nameof(CreateSetting), BindingFlags.NonPublic | BindingFlags.Instance)!,
            (x) => Settings.Add((IRuntimeSetting)x));

    public void MarkAsChanged()
        => Interlocked.Exchange(ref _changed, 1);

    protected virtual void OnFunctionBlockEnabledChanged() { }

    protected virtual void OnPersistentVariableChanged(IRuntimePersistentVariable variable) { }

    protected virtual void OnSettingsChanged() { }

    protected virtual void OnSettingChanged(IRuntimeSetting setting) { }

    // TODO: If we ever find a better name here, we can rename the method and no longer have to implement it explicitly.
    void IRuntimeFunctionBlock.ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (_functionBlockEnabledInput.Changed &&
            _functionBlockEnabledInput.Value != _functionBlockEnabledOutput.Value)
        {
            SetConnectorValueIfNotEqual(_functionBlockEnabledOutput, _functionBlockEnabledInput.Value, _functionBlockEnabledInput.Validity);
            OnFunctionBlockEnabledChanged();
        }

        ProcessCycle(cycleInfo);
    }

    protected virtual void ProcessCycle(IFunctionBlockCycleInfo cycleInfo) { }

    protected static bool SetConnectorValueIfNotEqual<T>(IRuntimeConnector<T> connector, T? value, Validity validity = Validity.Valid)
    {
        bool result = connector.SetValidityIfNotEqual(validity);
        result |= connector.SetValueIfNotEqual(value);
        return result;
    }

    protected static bool SetPersistentVariableIfNotEqual<T>(IRuntimePersistentVariable<T> variable, T? value)
        => variable.SetValueIfNotEqual(value);

    protected static bool SetSettingValueIfNotEqual<T>(IRuntimeSetting<T> setting, T? value)
        => setting.SetValueIfNotEqual(value);

    public void Setup(IFunctionBlock functionBlock, IRuntimeDriver? driver, HashSet<Guid> connectedConnectors, IUniqueNameProvider uniqueNameProvider, ILogger logger)
    {
        if (_setupCalled)
            throw new InvalidOperationException($"{nameof(Setup)} was already called. It is not allowed to call it again.");
        _setupCalled = true;

        FunctionBlock = functionBlock;
        if (FunctionBlock.Design.RuntimeDriverTypeName is not null && driver is null)
            throw new ArgumentNullException(nameof(driver));
        Driver = driver;
        _connectedConnectors = connectedConnectors;
        _uniqueNameProvider = uniqueNameProvider;
        Logger = logger;

        RunMode = FunctionBlock.RunMode;
        CycleFrequency = FunctionBlock.CycleFrequency;

        _functionBlockEnabledInput = CreateSystemInputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);
        _functionBlockEnabledOutput = CreateSystemOutputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);
        SystemInputs.Add(_functionBlockEnabledInput);
        SystemOutputs.Add(_functionBlockEnabledOutput);

        Settings.Changed += OnSettingsChanged;
        Settings.SettingChanged += OnSettingChanged;

        PersistentVariables.VariableChanged += OnPersistentVariableChanged;
    }

    protected virtual void ValidateConnectors()
    {
        if (FunctionBlock.ProcessDataInputs.SequenceEqualIgnoreOrder(ProcessDataInputs.Select(x => x.Connector)) == false)
            throw new InvalidOperationException("There are differences between inputs of design and implementation.");

        if (FunctionBlock.ProcessDataOutputs.SequenceEqualIgnoreOrder(ProcessDataOutputs.Select(x => x.Connector)) == false)
            throw new InvalidOperationException("There are differences between outputs of design and implementation.");
    }

    protected virtual void ValidatePersistentVariables()
    {
        if (FunctionBlock.PersistentVariables.SequenceEqualIgnoreOrder(PersistentVariables.Select(x => x.Variable)) == false)
            throw new InvalidOperationException("There are differences between persistent variables of design and implementation.");
    }

    protected virtual void ValidateSettings()
    {
        if (FunctionBlock.Settings.SequenceEqualIgnoreOrder(Settings.Select(x => x.Setting)) == false)
            throw new InvalidOperationException("There are differences between settings of design and implementation.");

        if (FunctionBlock.SettingsInputs.SequenceEqualIgnoreOrder(Settings.Select(x => x.Input.Connector)) == false)
            throw new InvalidOperationException("There are differences between settings inputs of design and implementation.");

        if (FunctionBlock.SettingsOutputs.SequenceEqualIgnoreOrder(Settings.Select(x => x.Output.Connector)) == false)
            throw new InvalidOperationException("There are differences between settings outputs of design and implementation.");
    }
}
