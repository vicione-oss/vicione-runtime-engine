using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeEngine : IDisposable
{
    ICommunicationManager? CommunicationManager { get; }
    IConnectorValueTransferInterceptor ConnectorValueTransferInterceptor { get; set; }
    IDataflow Dataflow { get; }
    IEnumerable<Assembly> DriverAssemblies { get; }
    IEnumerable<Assembly> FunctionBlockAssemblies { get; }
    ILoggerFactory LoggerFactory { get; }
    IPoolingValidator PoolingValidator { get; set; }
    IReferenceTypeCloneHandler ReferenceTypeCloneHandler { get; set; }
    IPersistence? SettingsPersistence { get; }
    EngineState State { get; }
    bool UseReferenceTypeCloneHandlerForLinks { get; set; }
    IPersistence? VariablesPersistence { get; }

    event EventHandler<ICycleInfo>? CycleFinished;
    event EventHandler<ICycleInfo>? CycleStarted;
    event EventHandler? StateChanged;

    ICycleInfo ProcessCycle();
    void Start();
    void Stop();
}
