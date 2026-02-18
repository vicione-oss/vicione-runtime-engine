using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.Runtime.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class RuntimeFunctionBlockExtensions
{
    extension(IRuntimeFunctionBlock block)
    {
        public IEnumerable<IRuntimeConnector> GetConnectors()
            => block.GetInputs().Concat(block.GetOutputs());

        public IEnumerable<IRuntimeConnector> GetInputs()
            => block.SettingsInputs.Concat(block.SystemInputs).Concat(block.ProcessDataInputs);

        public IEnumerable<IRuntimeConnector> GetOutputs()
            => block.SettingsOutputs.Concat(block.SystemOutputs).Concat(block.ProcessDataOutputs);
    }
}
