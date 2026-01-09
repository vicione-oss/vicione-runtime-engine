using System;
using System.Diagnostics;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed partial class RuntimeFunctionBlockProcessor
{
    private static class Telemetry
    {
        private static readonly string s_processRuntimeBlockActivityName = $"{EngineTelemetry.ActivitySourceName}.{nameof(RuntimeFunctionBlockProcessor)}.{nameof(ProcessBlocks)}.ProcessRuntimeFunctionBlock";
        private static readonly string s_processInputsActivityName = $"{EngineTelemetry.ActivitySourceName}.{nameof(RuntimeFunctionBlockProcessor)}.{nameof(ProcessBlocks)}.{nameof(ProcessInputs)}";
        private static readonly string s_processSettingsActivityName = $"{EngineTelemetry.ActivitySourceName}.{nameof(RuntimeFunctionBlockProcessor)}.{nameof(ProcessBlocks)}.{nameof(ProcessSettings)}";
        private static readonly string s_processBlockActivityName = $"{EngineTelemetry.ActivitySourceName}.{nameof(RuntimeFunctionBlockProcessor)}.{nameof(ProcessBlocks)}.{nameof(ProcessBlock)}";
        private static readonly string s_processOutputsActivityName = $"{EngineTelemetry.ActivitySourceName}.{nameof(RuntimeFunctionBlockProcessor)}.{nameof(ProcessBlocks)}.{nameof(ProcessOutputs)}";

        internal static Activity? StartForProcessRuntimeBlock(IFunctionBlock functionBlock, IUniqueNameProvider uniqueNameProvider)
        {
            Activity? activity = EngineTelemetry.ActivitySource.StartActivity(s_processRuntimeBlockActivityName);
            activity?.SetTag(EngineTelemetry.TagFunctionBlockId, functionBlock.Id);
            activity?.SetTag(EngineTelemetry.TagFunctionBlockName, uniqueNameProvider.GetUniqueName(functionBlock));
            activity?.SetTag(EngineTelemetry.TagFunctionBlockDesign, GetFunctionBlockDesignTag(functionBlock.Design));
            return activity;
        }

        internal static Activity? StartForProcessInputs()
            => EngineTelemetry.ActivitySource.StartActivity(s_processInputsActivityName);

        internal static Activity? StartForProcessSettings()
            => EngineTelemetry.ActivitySource.StartActivity(s_processSettingsActivityName);

        internal static Activity? StartForProcessBlock()
            => EngineTelemetry.ActivitySource.StartActivity(s_processBlockActivityName);

        internal static Activity? StartForProcessOutputs()
            => EngineTelemetry.ActivitySource.StartActivity(s_processOutputsActivityName);

        internal static void AddProcessedCount(Guid dataflowId, IFunctionBlock functionBlock, IUniqueNameProvider uniqueNameProvider)
            => EngineTelemetry.FunctionBlockProcessedCount.Add(1,
            [
                new(EngineTelemetry.TagDataflowId, dataflowId),
                new(EngineTelemetry.TagFunctionBlockId, functionBlock.Id),
                new(EngineTelemetry.TagFunctionBlockName, uniqueNameProvider.GetUniqueName(functionBlock)),
                new(EngineTelemetry.TagFunctionBlockDesign, GetFunctionBlockDesignTag(functionBlock.Design)),
            ]);

        private static string GetFunctionBlockDesignTag(IFunctionBlockDesign functionBlockDesign)
            => $"{functionBlockDesign.Namespace}.{functionBlockDesign.Name}";
    }
}
