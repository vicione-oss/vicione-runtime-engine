using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal static partial class RuntimeFunctionBlockProcessorLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while deciding whether the FB '{FunctionBlockId}' ({FunctionBlockName}) with type " +
        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}' should be processed: " +
        $"The {nameof(RunMode)} '{{RunModeName}}' is not implemented.")]
    public static partial void FunctionBlockRunModeNotImplemented(this ILogger logger, Guid functionBlockId, string functionBlockName,
        string functionBlockDesignNamespace, string functionBlockDesignName, RunMode runModeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while processing FB '{FunctionBlockId}' ({FunctionBlockName}) with type " +
        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}'.")]
    public static partial void FunctionBlockProcessingErrorOccured(this ILogger logger, Exception ex,
        Guid functionBlockId, string functionBlockName, string functionBlockDesignNamespace, string functionBlockDesignName);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while pooling input '{ConnectorId}' ({ConnectorName}) of FB " +
        "'{FunctionBlockId}' ({FunctionBlockName}) with Type " +
        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}'.")]
    public static partial void FunctionBlockInputConnectorPoolingErrorOccured(this ILogger logger, Exception ex, Guid connectorId, string connectorName,
        Guid functionBlockId, string functionBlockName, string functionBlockDesignNamespace, string functionBlockDesignName);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while transferring value of input '{ConnectorId}' ({ConnectorName}) of FB " +
        "'{FunctionBlockId}' ({FunctionBlockName}) with type " +
        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}' to outgoing links.")]
    public static partial void FunctionBlockInputConnectorValueTransferErrorOccured(this ILogger logger, Exception ex, Guid connectorId, string connectorName,
        Guid functionBlockId, string functionBlockName, string functionBlockDesignNamespace, string functionBlockDesignName);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while transferring value of output '{ConnectorId}' ({ConnectorName}) of FB " +
        "'{FunctionBlockId}' ({FunctionBlockName}) with type " +
        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}' to links.")]
    public static partial void FunctionBlockOutputConnectorValueTransferErrorOccured(this ILogger logger, Exception ex, Guid connectorId, string connectorName,
        Guid functionBlockId, string functionBlockName, string functionBlockDesignNamespace, string functionBlockDesignName);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while processing settings of FB '{FunctionBlockId}' ({FunctionBlockName}) with type " +
        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}'.")]
    public static partial void FunctionBlockSettingsProcessingErrorOccured(this ILogger logger, Exception ex,
        Guid functionBlockId, string functionBlockName, string functionBlockDesignNamespace, string functionBlockDesignName);
}
