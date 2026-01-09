using System.Collections.Generic;

namespace ViciOne.Core.Dataflow.DataModel.Extensions;

public static class DataflowExtensions
{
    public static Container? GetContainer(this Dataflow dataflow, FunctionBlock block)
    {
        foreach (Container container in GetContainers(dataflow))
            if (container.FunctionBlocks.Contains(block))
                return container;

        return null;
    }

    public static IEnumerable<Container> GetContainers(this Dataflow dataflow)
    {
        yield return dataflow.RootContainer;

        foreach (Container container in dataflow.RootContainer.GetContainers(true))
            yield return container;
    }
}
