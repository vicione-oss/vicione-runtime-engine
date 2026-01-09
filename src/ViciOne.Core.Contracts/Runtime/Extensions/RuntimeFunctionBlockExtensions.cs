using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.Runtime.Extensions;

public static class RuntimeFunctionBlockExtensions
{
    public static IEnumerable<IRuntimeConnector> GetConnectors(this IRuntimeFunctionBlock block)
        => GetInputs(block).Concat(GetOutputs(block));

    public static IEnumerable<IRuntimeConnector> GetInputs(this IRuntimeFunctionBlock block)
        => block.SettingsInputs.Concat(block.SystemInputs).Concat(block.ProcessDataInputs);

    public static IEnumerable<IRuntimeConnector> GetOutputs(this IRuntimeFunctionBlock block)
        => block.SettingsOutputs.Concat(block.SystemOutputs).Concat(block.ProcessDataOutputs);
}
