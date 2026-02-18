using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class FunctionBlockDesignExtensions
{
    extension(IFunctionBlockDesign design)
    {
        public IEnumerable<IConnectorDesign> GetConnectors()
            => design.GetInputs().Concat(design.GetOutputs());

        public IEnumerable<IConnectorDesign> GetInputs()
            => design.Settings.Select(x => x.Input).Concat(design.SystemInputs).Concat(design.ProcessDataInputs);

        public IEnumerable<IConnectorDesign> GetOutputs()
            => design.Settings.Select(x => x.Output).Concat(design.SystemOutputs).Concat(design.ProcessDataOutputs);

        public string GetRuntimeTypeName()
            => design.RuntimeTypeName ?? $"{design.Namespace}.{design.Name}";
    }
}
