using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimeFunctionBlockFactory(IEnumerable<Assembly> assemblies) :
    RuntimeElementFactory<(IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors), IFunctionBlockDesign, IRuntimeFunctionBlock>(assemblies)
{
    protected override Type ExpectedUnboundGenericBaseType => typeof(RuntimeFunctionBlock<>);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0008:Expliziten Typ verwenden", Justification = "Aufgrund des komplexen Tupels ist diese Variante deutlich lesbarer.")]
    public (IEnumerable<((IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors) BlockAndDriver, IRuntimeFunctionBlock RuntimeBlock)> Instances, IEnumerable<(IFunctionBlockDesign Design, Exception Exception)> FailedDesigns)
        CreateInstances(IEnumerable<(IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors)> functionBlocks, IUniqueNameProvider uniqueNameProvider, ILoggerFactory loggerFactory, ILogger logger)
    {
        var result = CreateInstancesBase(functionBlocks, uniqueNameProvider, loggerFactory);
        foreach ((IFunctionBlockDesign Design, Exception Exception) failedDesign in result.FailedDefinitions)
            logger.FunctionBlockInstanceCreationErrorOccured(failedDesign.Exception, failedDesign.Design.Namespace, failedDesign.Design.Name, failedDesign.Exception.Message);
        return result;
    }

    protected override ILogger CreateLogger((IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors) inputObject,
        IRuntimeFunctionBlock instance, IUniqueNameProvider uniqueNameProvider, ILoggerFactory loggerFactory)
        => loggerFactory.CreateLogger($"{uniqueNameProvider.GetUniqueName(inputObject.Block)} ({inputObject.Block.Id})");

    protected override IFunctionBlockDesign GetDefinition((IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors) inputObject)
        => inputObject.Block.Design;

    protected override bool IsRuntimeElementType(Type type, IFunctionBlockDesign definition)
        => string.Equals(type.FullName, definition.GetRuntimeTypeName(), StringComparison.Ordinal);

    protected override void SetupInstance((IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors) inputObject,
        IRuntimeFunctionBlock instance, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => instance.Setup(inputObject.Block, inputObject.Driver, inputObject.ConnectedConnectors, uniqueNameProvider, logger);
}
