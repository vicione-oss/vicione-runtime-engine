using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.Runtime;

namespace RuntimeDemo;

internal static partial class DemoCommunicationManagerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "State changed: {State}")]
    public static partial void EngineStateChanged(this ILogger logger, EngineState state);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting the engine. Press Ctrl+C to exit.")]
    public static partial void StartingEngine(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No matching setting was found in the dataflow for the stored setting with the ID '{SettingId}'. Loading this setting is skipped.")]
    public static partial void SettingNotFound(this ILogger logger, Guid settingId);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while loading the stored setting with the ID '{SettingId}'.")]
    public static partial void LoadSettingValueErrorOccured(this ILogger logger, Exception ex, Guid settingId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No matching variable was found in the dataflow for the stored variable with the ID '{PersistentVariableId}'. Loading this variable is skipped.")]
    public static partial void PersistentVariableNotFound(this ILogger logger, Guid persistentVariableId);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while loading the stored variable with the ID '{PersistentVariableId}'.")]
    public static partial void LoadPersistentVariableValueErrorOccured(this ILogger logger, Exception ex, Guid persistentVariableId);
}
