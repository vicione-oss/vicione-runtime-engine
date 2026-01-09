using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ViciOne.Engine;

public static class EngineTelemetry
{
    public static string ActivitySourceName { get; } = "ViciOne.Engine";
    public static string MeterName { get; } = ActivitySourceName;

    internal static ActivitySource ActivitySource { get; } = new(ActivitySourceName);
    internal static Meter Meter { get; } = new(MeterName);

    internal const string TagEngineCycleNumber = "engine.cycle.number";
    internal const string TagDataflowId = "dataflow.id";
    internal const string TagFunctionBlockId = "functionblock.id";
    internal const string TagFunctionBlockName = "functionblock.name";
    internal const string TagFunctionBlockCount = "functionblock.count";
    internal const string TagDriverCount = "driver.count";
    internal const string TagFunctionBlockDesign = "functionblock.design";

    internal static Counter<long> FunctionBlockProcessedCount { get; } = Meter.CreateCounter<long>(
        name: "engine.functionblock.processed.count",
        description: "Counts the number of processed FunctionBlocks in the engine.");
}
