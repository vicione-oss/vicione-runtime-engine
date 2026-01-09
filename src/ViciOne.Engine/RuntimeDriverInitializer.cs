using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimeDriverInitializer : RuntimeElementInitializer<IRuntimeDriver>
{
    public static RuntimeDriverInitializer Default { get; } = new RuntimeDriverInitializer();

    private RuntimeDriverInitializer() { }

    public (IEnumerable<IRuntimeDriver> Instances, IDictionary<IRuntimeDriver, Exception> FailedDrivers) InitializeInstancesI(IEnumerable<IRuntimeDriver> instances, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => InitializeInstance(instances, x => x.InitializeI(), 1, uniqueNameProvider, logger);

    public (IEnumerable<IRuntimeDriver> Instances, IDictionary<IRuntimeDriver, Exception> FailedDrivers) InitializeInstancesII(IEnumerable<IRuntimeDriver> instances, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => InitializeInstance(instances, x => x.InitializeII(), 2, uniqueNameProvider, logger);

    protected override void LogInitializationError(Exception exception, IRuntimeDriver element, byte phaseNo, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => logger.DriverInitializationErrorOccured(exception, phaseNo, element.GetType().FullName);
}
