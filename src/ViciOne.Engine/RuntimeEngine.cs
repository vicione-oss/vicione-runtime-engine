using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Contracts.DataModel.Filters;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public sealed partial class RuntimeEngine : IRuntimeEngine
{
    private RuntimeFunctionBlockProcessor? _blockProcessor;
    private CommunicationProcessor? _communicationProcessor;
    private readonly DefaultConnectorValueTransferInterceptor _defaultConnectorValueTransferInterceptor = new();
    private readonly DefaultReferenceTypeCloneHandler _defaultReferenceTypeCloneHandler = new();
    private bool _disposed;
    private readonly RuntimeDriverFactory _driverFactory;
    private readonly RuntimeFunctionBlockFactory _functionBlockFactory;
    private bool _isProcessing;
    private LinkCache? _linkCache;
    private readonly ILogger _logger;
    private PersistentVariablesPersistenceProcessor? _persistentVariablesPersistenceProcessor;
    private IRuntimeDriver[]? _runtimeDrivers;
    private IRuntimeFunctionBlock[]? _runtimeFunctionBlocks;
    private SettingsPersistenceProcessor? _settingsPersistenceProcessor;

    public ICommunicationManager? CommunicationManager { get; }
    public IConnectorValueTransferInterceptor ConnectorValueTransferInterceptor { get; set; }
    public IDataflow Dataflow { get; }
    public IEnumerable<Assembly> DriverAssemblies { get; }
    public IEnumerable<Assembly> FunctionBlockAssemblies { get; }
    public ILoggerFactory LoggerFactory { get; }
    public IPoolingValidator PoolingValidator { get; set; } = new DefaultPoolingValidator();
    public EngineQueueOptions QueueOptions { get; }
    public IReferenceTypeCloneHandler ReferenceTypeCloneHandler { get; set; }
    public IPersistence? SettingsPersistence { get; }
    public EngineState State { get; private set; }
    public IUniqueNameProvider UniqueNameProvider { get; }
    public bool UseReferenceTypeCloneHandlerForLinks { get; set; }
    public IPersistence? VariablesPersistence { get; }

    public event EventHandler<ICycleInfo>? CycleFinished;
    public event EventHandler<ICycleInfo>? CycleStarted;
    public event EventHandler? StateChanged;

    public RuntimeEngine(IDataflow dataflow, ILoggerFactory loggerFactory, IEnumerable<Assembly> driverAssemblies, IEnumerable<Assembly> functionBlockAssemblies, IUniqueNameProvider uniqueNameProvider,
        IPersistence? settingsPersistence = null, IPersistence? variablesPersistence = null, ICommunicationManager? communicationManager = null, EngineQueueOptions? queueOptions = null)
    {
        Dataflow = dataflow;
        LoggerFactory = loggerFactory;
        DriverAssemblies = driverAssemblies;
        FunctionBlockAssemblies = functionBlockAssemblies;
        UniqueNameProvider = uniqueNameProvider;
        SettingsPersistence = settingsPersistence;
        VariablesPersistence = variablesPersistence;
        CommunicationManager = communicationManager;
        QueueOptions = queueOptions ?? new();

        _logger = InitializeLogger();
        InitializeRuntimeElementFactories(out _driverFactory, out _functionBlockFactory);
        ChangeState(EngineState.Unstarted);
        ReferenceTypeCloneHandler = _defaultReferenceTypeCloneHandler;
        ConnectorValueTransferInterceptor = _defaultConnectorValueTransferInterceptor;
    }

    private void ChangeState(EngineState newState)
    {
        State = newState;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static CycleInfo CreateCycleInfo(ulong cycleNo, DateTime time)
        => new()
        {
            CycleNo = cycleNo,
            Time = time,
        };

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Stop();

            _defaultReferenceTypeCloneHandler.Dispose();
            _defaultConnectorValueTransferInterceptor.Dispose();

            CycleFinished = null;
            CycleStarted = null;
            StateChanged = null;
        }

        _disposed = true;
    }

    private void DisposeRuntimeDrivers()
    {
        if (_runtimeDrivers != null)
        {
            foreach (IRuntimeDriver runtimeDriver in _runtimeDrivers)
            {
                try { runtimeDriver.Dispose(); }
                catch (Exception ex) { _logger.DriverDisposalErrorOccured(ex, runtimeDriver.GetType().FullName); }
            }
            _runtimeDrivers = null;
        }
    }

    private void DisposeRuntimeFunctionBlocks()
    {
        _communicationProcessor?.Dispose();
        _communicationProcessor = null;
        _blockProcessor?.Dispose();
        _blockProcessor = null;
        _linkCache?.Dispose();
        _linkCache = null;
        if (_runtimeFunctionBlocks is not null)
        {
            foreach (IRuntimeFunctionBlock runtimeBlock in _runtimeFunctionBlocks)
            {
                try { runtimeBlock.Dispose(); }
                catch (Exception ex)
                {
                    _logger.FunctionBlockDisposalErrorOccured(ex, runtimeBlock.FunctionBlock.Id, UniqueNameProvider.GetUniqueName(runtimeBlock.FunctionBlock),
                        runtimeBlock.FunctionBlock.Design.Namespace, runtimeBlock.FunctionBlock.Design.Name);
                }
            }
            _runtimeFunctionBlocks = null;
        }
    }

    private void Initialize()
    {
        ChangeState(EngineState.Initializing);

        IFunctionBlock[] functionBlocks = [.. Dataflow.RootContainer.GetFunctionBlocks(true)];
        IEnumerable<string> driverTypeNames = DriverFilter.GetDistinctRuntimeDriverTypeNames(functionBlocks);

        (IEnumerable<(string DriverTypeName, IRuntimeDriver RuntimeDriver)> driverInstances, IEnumerable<(string, Exception)> failedDriverTypes) =
            _driverFactory.CreateInstances(driverTypeNames, UniqueNameProvider, LoggerFactory, _logger);
        _runtimeDrivers = [.. driverInstances.Select(x => x.RuntimeDriver)];
        if (failedDriverTypes.Any())
        {
            ChangeState(EngineState.InitializationError);
            return;
        }

        var connectedConnectors = Dataflow.Links.SelectMany(link => link.GetConnectedConnectors()).Select(connector => connector.Id).ToHashSet();
        IEnumerable<(IFunctionBlock, IRuntimeDriver?, HashSet<Guid>)> functionBlocksWithDrivers = functionBlocks
            .Select(x => (
                x,
                x.Design.RuntimeDriverTypeName is null ? null : driverInstances.First(y => string.Equals(y.DriverTypeName, x.Design.RuntimeDriverTypeName, StringComparison.Ordinal)).RuntimeDriver,
                connectedConnectors.Intersect(x.GetConnectors().Select(conn => conn.Id)).ToHashSet()));

        (IEnumerable<((IFunctionBlock, IRuntimeDriver?, HashSet<Guid>) Input, IRuntimeFunctionBlock RuntimeBlock)> blockInstances, IEnumerable<(IFunctionBlockDesign, Exception)> failedBlockDesigns) =
            _functionBlockFactory.CreateInstances(functionBlocksWithDrivers, UniqueNameProvider, LoggerFactory, _logger);
        _runtimeFunctionBlocks = [.. blockInstances.Select(x => x.RuntimeBlock)];
        if (failedBlockDesigns.Any())
        {
            ChangeState(EngineState.InitializationError);
            return;
        }

        if (InitializeInstances(_runtimeDrivers, _runtimeFunctionBlocks) == false)
        {
            ChangeState(EngineState.InitializationError);
            return;
        }

        _linkCache = InitializeLinkCache(_runtimeFunctionBlocks, Dataflow.Links);
        _blockProcessor = InitializeBlockProcessor(_runtimeFunctionBlocks, _linkCache);

        _settingsPersistenceProcessor = InitializeSettingsPersistenceProcessor(_runtimeFunctionBlocks);
        _persistentVariablesPersistenceProcessor = InitializePersistentVariablesPersistenceProcessor(_runtimeFunctionBlocks);

        _communicationProcessor = InitializeCommunicationProcessor(_linkCache);

        _communicationProcessor?.ProcessOutputCommunication(ReferenceTypeCloneHandler);

        ChangeState(EngineState.Initialized);
    }

    private RuntimeFunctionBlockProcessor InitializeBlockProcessor(IRuntimeFunctionBlock[] runtimeFunctionBlocks, LinkCache linkCache)
        => new(runtimeFunctionBlocks, linkCache, UniqueNameProvider, _logger, Dataflow.Id);

    private CommunicationProcessor? InitializeCommunicationProcessor(LinkCache linkCache)
        => CommunicationManager is not null
                ? new CommunicationProcessor(linkCache, UniqueNameProvider, CommunicationManager, QueueOptions.CommunicationReceiveBufferCapacity, _logger)
                : null;

    private bool InitializeInstances(IEnumerable<IRuntimeDriver> runtimeDrivers, IEnumerable<IRuntimeFunctionBlock> runtimeFunctionBlocks)
    {
        using Activity? initializeInstancesActivity = Telemetry.StartForInitializeInstances(Dataflow.Id, runtimeDrivers.Count(), runtimeFunctionBlocks.Count());

        if (RuntimeDriverInitializer.Default.InitializeInstancesI(runtimeDrivers, UniqueNameProvider, _logger).FailedDrivers.Any())
            return false;

        if (RuntimeFunctionBlockInitializer.Default.InitializeInstancesI(runtimeFunctionBlocks, UniqueNameProvider, _logger).FailedBlocks.Any())
            return false;

        if (RuntimeDriverInitializer.Default.InitializeInstancesII(runtimeDrivers, UniqueNameProvider, _logger).FailedDrivers.Any())
            return false;

        if (RuntimeFunctionBlockInitializer.Default.InitializeInstancesII(runtimeFunctionBlocks, UniqueNameProvider, _logger).FailedBlocks.Any())
            return false;

        return true;
    }

    private LinkCache InitializeLinkCache(IRuntimeFunctionBlock[] runtimeFunctionBlocks, IEnumerable<ILink> links)
        => new(runtimeFunctionBlocks, links, () => PoolingValidator, QueueOptions.PerLinkQueueCapacity);

    private ILogger InitializeLogger()
        => LoggerFactory.CreateLogger<RuntimeEngine>();

    private PersistentVariablesPersistenceProcessor? InitializePersistentVariablesPersistenceProcessor(IRuntimeFunctionBlock[] runtimeFunctionBlocks)
        => VariablesPersistence is not null
                ? new PersistentVariablesPersistenceProcessor(runtimeFunctionBlocks, UniqueNameProvider, VariablesPersistence, () => ReferenceTypeCloneHandler, _logger)
                : null;

    private void InitializeRuntimeElementFactories(out RuntimeDriverFactory driverFactory, out RuntimeFunctionBlockFactory functionBlockFactory)
    {
        driverFactory = new RuntimeDriverFactory(DriverAssemblies);
        functionBlockFactory = new RuntimeFunctionBlockFactory(FunctionBlockAssemblies);
    }

    private SettingsPersistenceProcessor? InitializeSettingsPersistenceProcessor(IRuntimeFunctionBlock[] runtimeFunctionBlocks)
        => SettingsPersistence is not null
                ? new SettingsPersistenceProcessor(runtimeFunctionBlocks, UniqueNameProvider, SettingsPersistence, () => ReferenceTypeCloneHandler, _logger)
                : null;

    public ICycleInfo ProcessCycle()
    {
        using Activity? cycleActivity = Telemetry.StartForProcessCycle(Dataflow.Id);

        if (State == EngineState.Starting)
            ChangeState(EngineState.Running);
        else if (State != EngineState.Running)
            throw new InvalidOperationException($"Invalid state. Only an Engine in state '{EngineState.Starting}' or '{EngineState.Running}' can be processed. This Engine´s state is '{State}'.");

        if (_isProcessing)
            throw new InvalidOperationException("Invalid call. The previous cycle must be finished before starting a new one.");
        _isProcessing = true;

        ulong cycleNo = _blockProcessor!.CycleNo;
        RaiseCycleStarted(CreateCycleInfo(cycleNo, DateTime.UtcNow));

        _communicationProcessor?.ProcessInputCommunication();
        _settingsPersistenceProcessor?.ApplyLoaded();
        _persistentVariablesPersistenceProcessor?.ApplyLoaded();
        _blockProcessor.ProcessBlocks(PoolingValidator, UseReferenceTypeCloneHandlerForLinks ? ReferenceTypeCloneHandler : null, ConnectorValueTransferInterceptor);
        _settingsPersistenceProcessor?.PersistChanged();
        _persistentVariablesPersistenceProcessor?.PersistChanged();
        _communicationProcessor?.ProcessOutputCommunication(ReferenceTypeCloneHandler);

        _isProcessing = false;
        CycleInfo finishedInfo = CreateCycleInfo(cycleNo, DateTime.UtcNow);

        Telemetry.AppendCycleNumber(cycleActivity, cycleNo);
        RaiseCycleFinished(finishedInfo);

        return finishedInfo;
    }

    private void RaiseCycleFinished(CycleInfo cycleInfo)
        => CycleFinished?.Invoke(this, cycleInfo);

    private void RaiseCycleStarted(CycleInfo cycleInfo)
        => CycleStarted?.Invoke(this, cycleInfo);

    public void Start()
    {
        if (State != EngineState.Unstarted)
            throw new InvalidOperationException($"Invalid state. Only an Engine in state '{EngineState.Unstarted}' can be started. This Engine´s state is '{State}'.");

        Initialize();
        if (State == EngineState.Initialized)
            ChangeState(EngineState.Starting);
    }

    public void Stop()
    {
        if (State is
            EngineState.Unstarted or
            EngineState.Stopping or
            EngineState.Stopped)
            return;

        while (State == EngineState.Initializing)
            Thread.Sleep(1);

        bool wasInitialized = State != EngineState.InitializationError;
        ChangeState(EngineState.Stopping);

        while (_isProcessing)
            Thread.Sleep(1);

        DisposeRuntimeFunctionBlocks();
        DisposeRuntimeDrivers();

        if (wasInitialized)
        {
            _settingsPersistenceProcessor?.PersistChanged();
            _settingsPersistenceProcessor?.Dispose();
            _settingsPersistenceProcessor = null;

            _persistentVariablesPersistenceProcessor?.PersistChanged();
            _persistentVariablesPersistenceProcessor?.Dispose();
            _persistentVariablesPersistenceProcessor = null;
        }

        ChangeState(EngineState.Stopped);
    }
}
