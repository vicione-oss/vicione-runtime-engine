using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class FunctionBlockExtensions
{
    extension(IFunctionBlock block)
    {
        public IEnumerable<IConnector> GetConnectors()
            => block.GetInputs().Concat(block.GetOutputs());

        public IEnumerable<IConnector> GetInputs()
            => block.SettingsInputs.Concat(block.SystemInputs).Concat(block.ProcessDataInputs);

        public IEnumerable<IConnector> GetOutputs()
            => block.SettingsOutputs.Concat(block.SystemOutputs).Concat(block.ProcessDataOutputs);
    }
}
