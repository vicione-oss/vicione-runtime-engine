using System.Collections.Generic;

namespace ViciOne.Core.Dataflow.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class DataflowExtensions
{
    extension(Dataflow dataflow)
    {
        public Container? GetContainer(FunctionBlock block)
        {
            foreach (Container container in dataflow.GetContainers())
                if (container.FunctionBlocks.Contains(block))
                    return container;

            return null;
        }

        public IEnumerable<Container> GetContainers()
        {
            yield return dataflow.RootContainer;

            foreach (Container container in dataflow.RootContainer.GetContainers(true))
                yield return container;
        }
    }
}
