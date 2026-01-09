using System;
using System.Diagnostics;

namespace ViciOne.Engine;

public partial class RuntimeEngine
{
    private static class Telemetry
    {
        private static readonly string s_cycleActivityName = $"{EngineTelemetry.ActivitySourceName}.{nameof(RuntimeEngine)}.{nameof(ProcessCycle)}";
        private static readonly string s_initializeInstancesActivityName = $"{EngineTelemetry.ActivitySourceName}.{nameof(RuntimeEngine)}.{nameof(InitializeInstances)}";

        internal static Activity? StartForInitializeInstances(Guid dataflowId, int runtimeDriverCount, int runtimeFunctionBlockCount)
        {
            Activity? activity = EngineTelemetry.ActivitySource.StartActivity(s_initializeInstancesActivityName);
            activity?.SetTag(EngineTelemetry.TagDataflowId, dataflowId);
            activity?.SetTag(EngineTelemetry.TagDriverCount, runtimeDriverCount);
            activity?.SetTag(EngineTelemetry.TagFunctionBlockCount, runtimeFunctionBlockCount);
            return activity;
        }

        internal static Activity? StartForProcessCycle(Guid dataflowId)
        {
            Activity? activity = EngineTelemetry.ActivitySource.StartActivity(s_cycleActivityName);
            activity?.SetTag(EngineTelemetry.TagDataflowId, dataflowId);
            return activity;
        }

        internal static Activity? AppendCycleNumber(Activity? activity, ulong cycleNo)
            => activity?.SetTag(EngineTelemetry.TagEngineCycleNumber, cycleNo);
    }
}
