using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;

namespace ViciOne.Core.Runtime.Execution;

public sealed class FunctionBlockExecutor : IDisposable
{
    private readonly InMemoryCommunicationManager _communication;
    private readonly DataflowInfo _dataflowInfo;
    private bool _disposed;
    private readonly Dictionary<Guid, IExternalIncomingLink> _inputIdsIncomingLinks = [];
    private readonly Dictionary<Guid, IConnector> _inputIdsInputs = [];
    private readonly Dictionary<Guid, ILinkChangedEntry> _inputOutgoingLinkValues = [];
    private readonly Dictionary<Guid, IConnector> _outgoingLinkIdsInputs = [];
    private readonly Dictionary<Guid, IConnector> _outgoingLinkIdsOutputs = [];
    private readonly Dictionary<Guid, ILinkChangedEntry> _outputOutgoingLinkValues = [];
    private readonly Dictionary<Guid, IPersistentVariable> _persistentVariableIdsPersistentVariables = [];
    private readonly Dictionary<Guid, IPersistenceEntry> _persistentVariableValues = [];
    private readonly InMemoryPersistence _persistentVariablesPersistence;
    private readonly RuntimeEngine _runtimeEngine;
    private bool _updatePersistedSettingsAfterNextCycle;
    private bool _updatePersistedVariablesAfterNextCycle;
    private readonly Dictionary<Guid, ISetting> _settingIdsSettings = [];
    private readonly Dictionary<Guid, IPersistenceEntry> _settingValues = [];
    private readonly InMemoryPersistence _settingsPersistence;
    private readonly DefaultUniqueNameProvider _uniqueNameProvider;

    public ulong CycleNo { get; private set; }
    public EngineState EngineState => _runtimeEngine.State;

    public event EventHandler<ICycleInfo>? CycleStarted;
    public event EventHandler<ICycleInfo>? CycleFinished;
    public event EventHandler? EngineStateChanged;
    public event EventHandler? InputValuesChanged;
    public event EventHandler? OutputValuesChanged;
    public event EventHandler? PersistentVariableValuesChanged;
    public event EventHandler? SettingValuesChanged;

    public FunctionBlockExecutor(DataflowInfo dataflowInfo, IEnumerable<Assembly> allAssemblies, ILoggerFactory loggerFactory)
    {
        _dataflowInfo = dataflowInfo;
        InitializeDictionaries(_dataflowInfo);

        IDataflow dataflow = _dataflowInfo.Dataflow;
        _communication = new InMemoryCommunicationManager();
        _communication.HandledExternalOutgoingLinkChangedEntriesChanged += Communication_HandledExternalOutgoingLinkChangedEntriesChanged;
        _settingsPersistence = new InMemoryPersistence();
        _settingsPersistence.EntriesAdded += SettingsPersistence_EntriesAdded;
        _persistentVariablesPersistence = new InMemoryPersistence();
        _persistentVariablesPersistence.EntriesAdded += PersistentVariablesPersistence_EntriesAdded;
        _uniqueNameProvider = new DefaultUniqueNameProvider(NameGenerator.CreateUniqueNames(dataflow));
        _runtimeEngine = new RuntimeEngine(dataflow, loggerFactory, allAssemblies, allAssemblies, _uniqueNameProvider, _settingsPersistence, _persistentVariablesPersistence, _communication);
        _runtimeEngine.CycleStarted += RuntimeEngine_CycleStarted;
        _runtimeEngine.CycleFinished += RuntimeEngine_CycleFinished;
        _runtimeEngine.StateChanged += RuntimeEngine_StateChanged;
    }

    public void ChangeInputValue(IConnector connector, object newValue)
    {
        IExternalIncomingLink inputLink = _inputIdsIncomingLinks[connector.Id];
        var changedEntry = new ChangedEntry()
        {
            Time = DateTime.UtcNow,
            Id = inputLink.Id,
            Validity = Validity.Valid,
            Value = newValue,
        };
        _communication.RaiseChangedEntriesReceived([changedEntry]);
    }

    public void ChangePersistentVariableValue(IPersistentVariable variable, object newValue)
    {
        var changedEntry = new ChangedEntry()
        {
            Time = DateTime.UtcNow,
            Id = variable.Id,
            Validity = Validity.Valid,
            Value = newValue,
        };
        _persistentVariablesPersistence.RaiseEntriesLoaded([changedEntry]);
        _updatePersistedVariablesAfterNextCycle = true;
    }

    public void ChangeSettingValue(ISetting setting, object newValue)
    {
        var changedEntry = new ChangedEntry()
        {
            Time = DateTime.UtcNow,
            Id = setting.Id,
            Validity = Validity.Valid,
            Value = newValue,
        };
        _settingsPersistence.RaiseEntriesLoaded([changedEntry]);
        _updatePersistedSettingsAfterNextCycle = true;
    }

    private void Communication_HandledExternalOutgoingLinkChangedEntriesChanged(object? sender, EventArgs e)
    {
        bool inputValuesChanged = false;
        bool outputValuesChanged = false;
        foreach (ILinkChangedEntry entry in _communication.GetAndResetHandledExternalOutgoingLinkChangedEntries())
            if (_inputOutgoingLinkValues.ContainsKey(entry.Id))
            {
                _inputOutgoingLinkValues[entry.Id] = entry;
                inputValuesChanged = true;
            }
            else if (_outputOutgoingLinkValues.ContainsKey(entry.Id))
            {
                _outputOutgoingLinkValues[entry.Id] = entry;
                outputValuesChanged = true;
            }

        if (inputValuesChanged)
            InputValuesChanged?.Invoke(this, EventArgs.Empty);
        if (outputValuesChanged)
            OutputValuesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _runtimeEngine.CycleStarted -= RuntimeEngine_CycleStarted;
            _runtimeEngine.CycleFinished -= RuntimeEngine_CycleFinished;
            _runtimeEngine.StateChanged -= RuntimeEngine_StateChanged;
            _runtimeEngine.Dispose();
            _persistentVariablesPersistence.EntriesAdded -= PersistentVariablesPersistence_EntriesAdded;
            _persistentVariablesPersistence.Dispose();
            _settingsPersistence.EntriesAdded -= SettingsPersistence_EntriesAdded;
            _settingsPersistence.Dispose();
            _communication.HandledExternalOutgoingLinkChangedEntriesChanged -= Communication_HandledExternalOutgoingLinkChangedEntriesChanged;
            _communication.Dispose();
        }

        _disposed = true;
    }

    public IEnumerable<IConnector> GetInputs()
        => _inputIdsInputs.Values;

    public IDictionary<IConnector, ChangedEntry> GetInputValues()
        => _inputOutgoingLinkValues
           .Select(x => (_outgoingLinkIdsInputs[x.Key], ChangedEntryFactory.Create(x.Value)))
           .ToDictionary(x => x.Item1, x => x.Item2);

    public IDictionary<IConnector, ChangedEntry> GetOutputValues()
       => _outputOutgoingLinkValues
           .Select(x => (_outgoingLinkIdsOutputs[x.Key], ChangedEntryFactory.Create(x.Value)))
           .ToDictionary(x => x.Item1, x => x.Item2);

    public IEnumerable<IPersistentVariable> GetPersistentVariables()
        => _persistentVariableIdsPersistentVariables.Values;

    public IEnumerable<ISetting> GetSettings()
        => _settingIdsSettings.Values;

    public IDictionary<IPersistentVariable, ChangedEntry> GetPersistentVariableValues()
        => _persistentVariableValues
           .Select(x => (_persistentVariableIdsPersistentVariables[x.Key], ChangedEntryFactory.Create(x.Value)))
           .ToDictionary(x => x.Item1, x => x.Item2);

    public IDictionary<ISetting, ChangedEntry> GetSettingValues()
        => _settingValues
           .Select(x => (_settingIdsSettings[x.Key], ChangedEntryFactory.Create(x.Value)))
           .ToDictionary(x => x.Item1, x => x.Item2);

    private void InitializeDictionaries(DataflowInfo info)
    {
        foreach (ISetting setting in info.Settings)
        {
            _settingIdsSettings[setting.Id] = setting;
            _settingValues[setting.Id] = new ChangedEntry() { Value = setting.Value };
        }
        foreach (IPersistentVariable variable in info.PersistentVariables)
        {
            _persistentVariableIdsPersistentVariables[variable.Id] = variable;
            _persistentVariableValues[variable.Id] = new ChangedEntry() { Value = variable.Value };
        }
        foreach (InputConnectorInfo input in info.Inputs)
        {
            _inputIdsInputs[input.Connector.Id] = input.Connector;
            _inputIdsIncomingLinks[input.Connector.Id] = input.IncomingLink;
            _inputOutgoingLinkValues[input.OutgoingLink.Id] = new ChangedEntry() { Value = input.Connector.Value };
            _outgoingLinkIdsInputs[input.OutgoingLink.Id] = input.Connector;
        }
        foreach (OutputConnectorInfo output in info.Outputs)
        {
            _outgoingLinkIdsOutputs[output.OutgoingLink.Id] = output.Connector;
            _outputOutgoingLinkValues[output.OutgoingLink.Id] = new ChangedEntry() { Value = output.Connector.Value };
        }
    }

    private void PersistentVariablesPersistence_EntriesAdded(object? sender, EventArgs e)
        => StorePersistedVariables(_persistentVariablesPersistence.GetAndResetPersisted());

    private void RuntimeEngine_CycleStarted(object? sender, ICycleInfo e)
    {
        CycleNo = e.CycleNo;
        CycleStarted?.Invoke(this, e);
    }

    private void RuntimeEngine_CycleFinished(object? sender, ICycleInfo e)
    {
        UpdatePersistedSettingsIfNecessary();
        UpdatePersistedVariablesIfNecessary();
        CycleFinished?.Invoke(this, e);
    }

    private void UpdatePersistedSettingsIfNecessary()
    {
        if (_updatePersistedSettingsAfterNextCycle == false)
            return;
        _updatePersistedSettingsAfterNextCycle = false;
        StorePersistedSettings(_settingsPersistence.RequestAllValues());
    }

    private void UpdatePersistedVariablesIfNecessary()
    {
        if (_updatePersistedVariablesAfterNextCycle == false)
            return;
        _updatePersistedVariablesAfterNextCycle = false;
        StorePersistedVariables(_persistentVariablesPersistence.RequestAllValues());
    }

    private void RuntimeEngine_StateChanged(object? sender, EventArgs e)
        => EngineStateChanged?.Invoke(this, e);

    private void SettingsPersistence_EntriesAdded(object? sender, EventArgs e)
        => StorePersistedSettings(_settingsPersistence.GetAndResetPersisted());

    public void ProcessCycle()
        => _runtimeEngine.ProcessCycle();

    public void StartEngine()
        => _runtimeEngine.Start();

    public void StopEngine()
        => _runtimeEngine.Stop();

    private void StorePersistedSettings(IPersistenceEntry[] entries)
    {
        foreach (IPersistenceEntry entry in entries)
            _settingValues[entry.Id] = entry;
        if (entries.Length > 0)
            SettingValuesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StorePersistedVariables(IPersistenceEntry[] entries)
    {
        foreach (IPersistenceEntry entry in entries)
            _persistentVariableValues[entry.Id] = entry;
        if (entries.Length > 0)
            PersistentVariableValuesChanged?.Invoke(this, EventArgs.Empty);
    }
}
