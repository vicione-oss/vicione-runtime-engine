using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public static class FunctionBlockDesignDefaults
{
    public static uint CycleFrequency { get; } = 1;
    public static RunMode RunMode { get; } = RunMode.Change;
    public static IEnumerable<RunMode> ValidRunModes { get; } = [RunMode.Change];
}
