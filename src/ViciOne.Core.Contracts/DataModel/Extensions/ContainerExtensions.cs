using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class ContainerExtensions
{
    extension(IContainer container)
    {
        public IEnumerable<IConnector> GetConnectors()
            => container.Inputs.Concat(container.Outputs);

        public IEnumerable<IContainer> GetContainers(bool includeNested = false)
        {
            foreach (IContainer child in container.Containers)
            {
                yield return child;

                if (includeNested)
                    foreach (IContainer recursiveChild in child.GetContainers(includeNested))
                        yield return recursiveChild;
            }
        }

        public IEnumerable<IFunctionBlock> GetFunctionBlocks(bool includeNested = false)
        {
            foreach (IFunctionBlock functionBlock in container.FunctionBlocks)
                yield return functionBlock;

            if (includeNested)
                foreach (IContainer childContainer in container.Containers)
                    foreach (IFunctionBlock childFunctionBlock in childContainer.GetFunctionBlocks(includeNested))
                        yield return childFunctionBlock;
        }
    }
}
