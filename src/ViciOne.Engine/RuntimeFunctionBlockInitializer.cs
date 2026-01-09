using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimeFunctionBlockInitializer : RuntimeElementInitializer<IRuntimeFunctionBlock>
{
    public static RuntimeFunctionBlockInitializer Default { get; } = new RuntimeFunctionBlockInitializer();

    private RuntimeFunctionBlockInitializer() { }

    public (IEnumerable<IRuntimeFunctionBlock> Instances, IDictionary<IRuntimeFunctionBlock, Exception> FailedBlocks)
        InitializeInstancesI(IEnumerable<IRuntimeFunctionBlock> instances, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => InitializeInstance(instances, x => x.InitializeI(), 1, uniqueNameProvider, logger);

    public (IEnumerable<IRuntimeFunctionBlock> Instances, IDictionary<IRuntimeFunctionBlock, Exception> FailedBlocks)
        InitializeInstancesII(IEnumerable<IRuntimeFunctionBlock> instances, IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => InitializeInstance(instances, x => x.InitializeII(), 2, uniqueNameProvider, logger);

    protected override void LogInitializationError(Exception exception, IRuntimeFunctionBlock element, byte phaseNo,
        IUniqueNameProvider uniqueNameProvider, ILogger logger)
        => logger.FunctionBlockInitializationErrorOccured(exception,
            phaseNo, element.FunctionBlock.Id, uniqueNameProvider.GetUniqueName(element.FunctionBlock),
            element.FunctionBlock.Design.Namespace, element.FunctionBlock.Design.Name);
}
