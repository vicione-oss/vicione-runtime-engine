using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Engine;
using ViciOne.Serialization.Json;

namespace RuntimeDemo;

internal sealed class Program
{
    private static Guid? s_guidForInterceptor1;
    private static Guid? s_guidForInterceptor2;
    private static Guid? s_guidForInterceptor3;

    private static void Main(string[] args)
    {
        string dataflowPath = args[0];
        string dllDir = args[1];
        string persistedSettingsPath = args[2];
        string persistedVariablesPath = args[3];
        string communicationPath = args[4];
        bool showOutgoingExternalLinkValuesInConsole = args[5] == "1";

        // Engine Aufgaben
        Assembly[] assemblies = [.. Directory.GetFiles(dllDir, "*.dll", SearchOption.AllDirectories).Select(Assembly.LoadFrom)];
        IDataflow dataflow = Load<Dataflow>(dataflowPath);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));
        ILogger persistenceLogger = loggerFactory.CreateLogger<IPersistence>();
        IFunctionBlock[] functionBlocks = [.. dataflow.RootContainer.GetFunctionBlocks(true)];
        DemoPersistence settingsPersistence = CreatePersistence(persistedSettingsPath, persistenceLogger);
        DemoPersistence variablesPersistence = CreatePersistence(persistedVariablesPath, persistenceLogger);

        using DemoCommunicationManager communicationManager = CreateCommunicationManager(communicationPath, functionBlocks, dataflow.Links, loggerFactory.CreateLogger<ICommunicationManager>(), showOutgoingExternalLinkValuesInConsole);
        InjectPersistedSettings(functionBlocks, settingsPersistence, persistenceLogger);
        InjectPersistedVariables(functionBlocks, variablesPersistence, persistenceLogger);
        IUniqueNameProvider uniqueNameProvider = new DefaultUniqueNameProvider(NameGenerator.CreateUniqueNames(dataflow));
        ExecuteEngine(dataflow, functionBlocks, assemblies, uniqueNameProvider, settingsPersistence, variablesPersistence, communicationManager, loggerFactory);
    }

    private static DemoCommunicationManager CreateCommunicationManager(string communicationPath, IEnumerable<IFunctionBlock> functionBlocks, IEnumerable<ILink> links, ILogger logger, bool showOutgoingExternalLinkValuesInConsole)
        => new(communicationPath, functionBlocks, links, logger, showOutgoingExternalLinkValuesInConsole);

    private static DemoPersistence CreatePersistence(string persistedSettingsPath, ILogger logger)
        => new(persistedSettingsPath, logger);

    private static void ExecuteEngine(IDataflow dataflow, IEnumerable<IFunctionBlock> functionBlocks, IEnumerable<Assembly> assemblies, IUniqueNameProvider uniqueNameProvider, IPersistence settingsPersistence, IPersistence variablesPersistence, ICommunicationManager communicationManager, ILoggerFactory loggerFactory)
    {
        ILogger engineLogger = loggerFactory.CreateLogger<IRuntimeEngine>();

        using var timer = new DemoTimer();
        using var runtimeEngine = new RuntimeEngine(dataflow, loggerFactory, assemblies, assemblies, uniqueNameProvider, settingsPersistence, variablesPersistence, communicationManager);
        var interceptor = new DefaultConnectorValueTransferInterceptor();
        FindGuidsForInterceptor(functionBlocks);
        runtimeEngine.ConnectorValueTransferInterceptor = interceptor;

        void StateChanged(object? sender, EventArgs e) => engineLogger.EngineStateChanged(runtimeEngine.State);
        runtimeEngine.StateChanged += StateChanged;
        StateChanged(null, EventArgs.Empty);

        void ConsoleCancel(object? sender, ConsoleCancelEventArgs e)
        {
            timer.Stop();
            runtimeEngine.Stop();
            e.Cancel = true;
        }
        Console.CancelKeyPress += ConsoleCancel;
        engineLogger.StartingEngine();

        runtimeEngine.Start();

        timer.Interval = 100;
        timer.Tick += (_, __) => runtimeEngine.ProcessCycle();
        timer.Start();

        while (runtimeEngine.State != EngineState.Stopped)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.R)
                    RegisterInterceptors(interceptor);
                else if (key.Key == ConsoleKey.U)
                    UnregisterInterceptors(interceptor);
                else if (key.Key == ConsoleKey.I)
                    SignalInterceptionsPending(interceptor);
            }

            Thread.Sleep(100);
        }
        runtimeEngine.StateChanged -= StateChanged;

        Console.CancelKeyPress -= ConsoleCancel;
    }

    private static IConnector? FindConnector(string fbName, string connectorName, bool isInput, IEnumerable<IFunctionBlock> fbs)
        => fbs
            .Where(x => string.Equals(x.Name, fbName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => isInput ? x.GetInputs() : x.GetOutputs())
            .FirstOrDefault(x => string.Equals(x.Design.Name, connectorName, StringComparison.OrdinalIgnoreCase));

    private static void InjectPersisted<TPersistable>(IEnumerable<IFunctionBlock> functionBlocks, DemoPersistence persistence,
        Func<IFunctionBlock, IEnumerable<TPersistable>> selectPersistable, Action<TPersistable, IPersistenceEntry> applyLoadedData,
        Action<IPersistenceEntry> logNotFound, Action<IPersistenceEntry, Exception> logApplyError) where
        TPersistable : IPersistable
    {
        IEnumerable<IPersistenceEntry> loadedEntries = persistence.GetPersisted();
        var persistables = functionBlocks
            .SelectMany(x => selectPersistable(x))
            .ToDictionary(
                x => x.Id,
                x => x);
        foreach (IPersistenceEntry loadedEntry in loadedEntries)
        {
            if (persistables.TryGetValue(loadedEntry.Id, out TPersistable? persistable) == false)
            {
                logNotFound(loadedEntry);
                continue;
            }

            if (loadedEntry.Time > persistable.ChangedTime)
                try
                {
                    applyLoadedData(persistable, loadedEntry);
                }
                catch (Exception ex)
                {
                    logApplyError(loadedEntry, ex);
                }
        }
    }

    private static void FindGuidsForInterceptor(IEnumerable<IFunctionBlock> functionBlocks)
    {
        // Objekte aus dataflow1
        s_guidForInterceptor1 = FindConnector("Source 0", "Sum", false, functionBlocks)?.Id;
        s_guidForInterceptor2 = FindConnector("Target", "A", true, functionBlocks)?.Id;
        s_guidForInterceptor3 = FindConnector("Incrementor", "Value", false, functionBlocks)?.Id;
    }

    private static void InjectPersistedSettings(IEnumerable<IFunctionBlock> functionBlocks, DemoPersistence persistence, ILogger logger)
        => InjectPersisted(functionBlocks, persistence,
            fb => fb.Settings,
            (setting, loadedEntry) =>
            {
                setting.Value = loadedEntry.Value;
                setting.ChangedTime = loadedEntry.Time;
                setting.Input.Value = loadedEntry.Value;
                setting.Output.Value = loadedEntry.Value;
            },
            loadedEntry => logger.SettingNotFound(loadedEntry.Id),
            (loadedEntry, ex) => logger.LoadSettingValueErrorOccured(ex, loadedEntry.Id));

    private static void InjectPersistedVariables(IEnumerable<IFunctionBlock> functionBlocks, DemoPersistence persistence, ILogger logger)
        => InjectPersisted(functionBlocks, persistence,
            fb => fb.PersistentVariables,
            (variable, loadedEntry) =>
            {
                variable.Value = loadedEntry.Value;
                variable.ChangedTime = loadedEntry.Time;
            },
            loadedEntry => logger.PersistentVariableNotFound(loadedEntry.Id),
            (loadedEntry, ex) => logger.LoadPersistentVariableValueErrorOccured(ex, loadedEntry.Id));

    private static T Load<T>(string path)
        => JsonSerialization.Default.LoadFromFile<T>(path);

    private static void RegisterInterceptors(DefaultConnectorValueTransferInterceptor interceptor)
    {
        if (s_guidForInterceptor1.HasValue)
            interceptor.RegisterInterceptor<double>(s_guidForInterceptor1.Value, (value) => new ConnectorValueTransferInterceptionResult<double>(value + 42));
        if (s_guidForInterceptor2.HasValue)
            interceptor.RegisterInterceptor<bool>(s_guidForInterceptor2.Value, (value) => new ConnectorValueTransferInterceptionResult<bool>(false));
        if (s_guidForInterceptor3.HasValue)
            interceptor.RegisterInterceptor<double>(s_guidForInterceptor3.Value, (value) => new ConnectorValueTransferInterceptionResult<double>(value + 100));
    }

    private static void SignalInterceptionsPending(DefaultConnectorValueTransferInterceptor interceptor)
    {
        if (s_guidForInterceptor1.HasValue)
            interceptor.SignalInterceptionPending(s_guidForInterceptor1.Value);
        if (s_guidForInterceptor2.HasValue)
            interceptor.SignalInterceptionPending(s_guidForInterceptor2.Value);
        if (s_guidForInterceptor3.HasValue)
            interceptor.SignalInterceptionPending(s_guidForInterceptor3.Value);
    }

    private static void UnregisterInterceptors(DefaultConnectorValueTransferInterceptor interceptor)
    {
        if (s_guidForInterceptor1.HasValue)
            interceptor.UnregisterInterceptor(s_guidForInterceptor1.Value);
        if (s_guidForInterceptor2.HasValue)
            interceptor.UnregisterInterceptor(s_guidForInterceptor2.Value);
        if (s_guidForInterceptor3.HasValue)
            interceptor.UnregisterInterceptor(s_guidForInterceptor3.Value);
    }

    private sealed class ConnectorValueTransferInterceptionResult<TValue>(TValue value) : IConnectorValueTransferInterceptionResult<TValue>
    {
        public bool CancelValueTransfer { get; set; }
        public TValue Value { get; } = value;
    }
}
