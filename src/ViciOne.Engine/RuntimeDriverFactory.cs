using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimeDriverFactory(IEnumerable<Assembly> assemblies) : RuntimeElementFactory<string, string, IRuntimeDriver>(assemblies)
{
    protected override Type ExpectedUnboundGenericBaseType => typeof(RuntimeDriver<>);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0008:Use explicit type", Justification = "Because of the complex tuple this variant is considerably more readable.")]
    public (IEnumerable<(string DriverTypeName, IRuntimeDriver RuntimeDriver)> Instances, IEnumerable<(string DriverTypeName, Exception Exception)> FailedDriverTypes)
        CreateInstances(IEnumerable<string> driverTypeNames, IUniqueNameProvider uniqueNameProvider, ILoggerFactory loggerFactory, ILogger logger)
    {
        var result = CreateInstancesBase(driverTypeNames, uniqueNameProvider, loggerFactory);
        foreach ((string DriverTypeName, Exception Exception) failedDriverInfos in result.FailedDefinitions)
            logger.DriverInstanceCreationErrorOccured(failedDriverInfos.Exception, failedDriverInfos.DriverTypeName, failedDriverInfos.Exception.Message);
        return result;
    }

    protected override ILogger CreateLogger(string inputObject, IRuntimeDriver instance, IUniqueNameProvider uniqueNameProvider, ILoggerFactory loggerFactory)
        => loggerFactory.CreateLogger(instance.GetType().FullName!);

    protected override string GetDefinition(string inputObject)
        => inputObject;

    protected override bool IsRuntimeElementType(Type type, string definition)
        => string.Equals(type.FullName, definition, StringComparison.Ordinal);

    protected override void SetupInstance(string inputObject, IRuntimeDriver instance, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => instance.Setup(logger);
}
