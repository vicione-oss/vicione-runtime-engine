using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

public static class FunctionBlockDesignExtensions
{
    public static IEnumerable<IConnectorDesign> GetConnectors(this IFunctionBlockDesign design)
        => GetInputs(design).Concat(GetOutputs(design));

    public static IEnumerable<IConnectorDesign> GetInputs(this IFunctionBlockDesign design)
        => design.Settings.Select(x => x.Input).Concat(design.SystemInputs).Concat(design.ProcessDataInputs);

    public static IEnumerable<IConnectorDesign> GetOutputs(this IFunctionBlockDesign design)
        => design.Settings.Select(x => x.Output).Concat(design.SystemOutputs).Concat(design.ProcessDataOutputs);

    public static string GetRuntimeTypeName(this IFunctionBlockDesign design)
        => design.RuntimeTypeName ?? $"{design.Namespace}.{design.Name}";
}
