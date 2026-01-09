using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

public static class FunctionBlockExtensions
{
    public static IEnumerable<IConnector> GetConnectors(this IFunctionBlock block)
        => GetInputs(block).Concat(GetOutputs(block));

    public static IEnumerable<IConnector> GetInputs(this IFunctionBlock block)
        => block.SettingsInputs.Concat(block.SystemInputs).Concat(block.ProcessDataInputs);

    public static IEnumerable<IConnector> GetOutputs(this IFunctionBlock block)
        => block.SettingsOutputs.Concat(block.SystemOutputs).Concat(block.ProcessDataOutputs);
}
