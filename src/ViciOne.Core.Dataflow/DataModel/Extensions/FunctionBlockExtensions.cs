using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Dataflow.DataModel.Extensions;

public static class FunctionBlockExtensions
{
    public static IEnumerable<Connector> GetConnectors(this FunctionBlock block)
        => GetInputs(block).Concat(GetOutputs(block));

    public static IEnumerable<Connector> GetInputs(this FunctionBlock block)
        => block.SettingsInputs.Concat(block.SystemInputs).Concat(block.ProcessDataInputs);

    public static IEnumerable<Connector> GetOutputs(this FunctionBlock block)
        => block.SettingsOutputs.Concat(block.SystemOutputs).Concat(block.ProcessDataOutputs);
}
