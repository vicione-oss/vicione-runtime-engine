using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Dataflow.DataModel.Extensions;

public static class FunctionBlockDesignExtensions
{
    public static IEnumerable<ConnectorDesign> GetConnectors(this FunctionBlockDesign design)
        => GetInputs(design).Concat(GetOutputs(design));

    public static IEnumerable<ConnectorDesign> GetInputs(this FunctionBlockDesign design)
        => design.Settings.Select(x => x.Input).Concat(design.SystemInputs).Concat(design.ProcessDataInputs);

    public static IEnumerable<ConnectorDesign> GetOutputs(this FunctionBlockDesign design)
        => design.Settings.Select(x => x.Output).Concat(design.SystemOutputs).Concat(design.ProcessDataOutputs);
}
