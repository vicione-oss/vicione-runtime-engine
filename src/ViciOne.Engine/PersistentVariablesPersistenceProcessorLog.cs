using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class PersistentVariablesPersistenceProcessorLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while applying loaded value of variable '{PersistentVariableId}' ({PersistentVariableName}).")]
    public static partial void ApplyLoadedPersistentVariableValueErrorOccured(this ILogger logger, Exception ex, Guid persistentVariableId, string persistentVariableName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Can't find variable '{PersistentVariableId}'. Applying loaded value will be skipped for this variable.")]
    public static partial void NoPersistentVariableWithIdFound(this ILogger logger, Guid persistentVariableId);
}
