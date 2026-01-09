using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

public static class DataflowExtensions
{
    public static IContainer? GetContainer(this IDataflow dataflow, IFunctionBlock block)
    {
        foreach (IContainer container in GetContainers(dataflow))
            if (container.FunctionBlocks.Contains(block))
                return container;

        return null;
    }

    public static IEnumerable<IContainer> GetContainers(this IDataflow dataflow)
    {
        yield return dataflow.RootContainer;

        foreach (IContainer container in dataflow.RootContainer.GetContainers(true))
            yield return container;
    }
}
