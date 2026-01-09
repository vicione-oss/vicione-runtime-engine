using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

public static class ContainerExtensions
{
    public static IEnumerable<IConnector> GetConnectors(this IContainer container)
        => container.Inputs.Concat(container.Outputs);

    public static IEnumerable<IContainer> GetContainers(this IContainer container, bool includeNested = false)
    {
        foreach (IContainer child in container.Containers)
        {
            yield return child;

            if (includeNested)
                foreach (IContainer recursiveChild in GetContainers(child, includeNested))
                    yield return recursiveChild;
        }
    }

    public static IEnumerable<IFunctionBlock> GetFunctionBlocks(this IContainer container, bool includeNested = false)
    {
        foreach (IFunctionBlock functionBlock in container.FunctionBlocks)
            yield return functionBlock;

        if (includeNested)
            foreach (IContainer childContainer in container.Containers)
                foreach (IFunctionBlock childFunctionBlock in GetFunctionBlocks(childContainer, includeNested))
                    yield return childFunctionBlock;
    }
}
