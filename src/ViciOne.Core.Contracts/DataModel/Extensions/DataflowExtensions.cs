using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class DataflowExtensions
{
    extension(IDataflow dataflow)
    {
        public IContainer? GetContainer(IFunctionBlock block)
        {
            foreach (IContainer container in dataflow.GetContainers())
                if (container.FunctionBlocks.Contains(block))
                    return container;

            return null;
        }

        public IEnumerable<IContainer> GetContainers()
        {
            yield return dataflow.RootContainer;

            foreach (IContainer container in dataflow.RootContainer.GetContainers(true))
                yield return container;
        }
    }
}
