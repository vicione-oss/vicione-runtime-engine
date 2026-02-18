using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Dataflow.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class FunctionBlockDesignExtensions
{
    extension(FunctionBlockDesign design)
    {
        public IEnumerable<ConnectorDesign> GetConnectors()
            => design.GetInputs().Concat(design.GetOutputs());

        public IEnumerable<ConnectorDesign> GetInputs()
            => design.Settings.Select(x => x.Input).Concat(design.SystemInputs).Concat(design.ProcessDataInputs);

        public IEnumerable<ConnectorDesign> GetOutputs()
            => design.Settings.Select(x => x.Output).Concat(design.SystemOutputs).Concat(design.ProcessDataOutputs);
    }
}
