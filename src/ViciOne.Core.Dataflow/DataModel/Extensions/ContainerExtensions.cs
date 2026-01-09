using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Dataflow.DataModel.Extensions;

public static class ContainerExtensions
{
    public static IEnumerable<Connector> GetConnectors(this Container container)
        => container.Inputs.Concat(container.Outputs);

    public static IEnumerable<Container> GetContainers(this Container container, bool includeNested = false)
    {
        foreach (Container child in container.Containers)
        {
            yield return child;

            if (includeNested)
                foreach (Container recursiveChild in GetContainers(child, includeNested))
                    yield return recursiveChild;
        }
    }

    public static IEnumerable<FunctionBlock> GetFunctionBlocks(this Container container, bool includeNested = false)
    {
        foreach (FunctionBlock functionBlock in container.FunctionBlocks)
            yield return functionBlock;

        if (includeNested)
            foreach (Container childContainer in container.Containers)
                foreach (FunctionBlock childFunctionBlock in GetFunctionBlocks(childContainer, includeNested))
                    yield return childFunctionBlock;
    }
}
