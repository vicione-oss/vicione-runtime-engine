using Microsoft.Extensions.Logging;

internal interface IHasLogger
{
    ILogger Logger { get; }
}
