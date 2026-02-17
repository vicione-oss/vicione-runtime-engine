using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Dataflow.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class ContainerExtensions
{
    extension(Container container)
    {
        public IEnumerable<Connector> GetConnectors()
            => container.Inputs.Concat(container.Outputs);

        public IEnumerable<Container> GetContainers(bool includeNested = false)
        {
            foreach (Container child in container.Containers)
            {
                yield return child;

                if (includeNested)
                    foreach (Container recursiveChild in child.GetContainers(includeNested))
                        yield return recursiveChild;
            }
        }

        public IEnumerable<FunctionBlock> GetFunctionBlocks(bool includeNested = false)
        {
            foreach (FunctionBlock functionBlock in container.FunctionBlocks)
                yield return functionBlock;

            if (includeNested)
                foreach (Container childContainer in container.Containers)
                    foreach (FunctionBlock childFunctionBlock in childContainer.GetFunctionBlocks(includeNested))
                        yield return childFunctionBlock;
        }
    }
}
