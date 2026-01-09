using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.Runtime;

namespace RuntimeDemo;

internal static partial class DemoCommunicationManagerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Zustand geändert: {State}")]
    public static partial void EngineStateChanged(this ILogger logger, EngineState state);

    [LoggerMessage(Level = LogLevel.Information, Message = "Engine wird gestartet. Strg+C drücken zum Beenden.")]
    public static partial void StartingEngine(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Für die gespeicherte Einstellung mit der ID '{SettingId}' wurde keine entsprechende Einstellung im Dataflow gefunden. Das Laden dieser Einstellung wird übersprungen.")]
    public static partial void SettingNotFound(this ILogger logger, Guid settingId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Beim Laden der gespeicherten Einstellung mit der ID '{SettingId}' ist ein Fehler aufgetreten.")]
    public static partial void LoadSettingValueErrorOccured(this ILogger logger, Exception ex, Guid settingId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Für die gespeicherte Variable mit der ID '{PersistentVariableId}' wurde keine entsprechende Variable im Dataflow gefunden. Das Laden dieser Variable wird übersprungen.")]
    public static partial void PersistentVariableNotFound(this ILogger logger, Guid persistentVariableId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Beim Laden der gespeicherten Variable mit der ID '{PersistentVariableId}' ist ein Fehler aufgetreten.")]
    public static partial void LoadPersistentVariableValueErrorOccured(this ILogger logger, Exception ex, Guid persistentVariableId);
}
