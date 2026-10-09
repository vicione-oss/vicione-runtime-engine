using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public sealed class RuntimeLinkFactory(int queueCapacity, ILogger logger) : IDisposable
{
    private bool _disposed;
    private readonly ExternalIncomingRuntimeLinkFactory _externalIncomingRuntimeLinkFactory = new(queueCapacity, logger);
    private readonly ExternalOutgoingRuntimeLinkFactory _externalOutgoingRuntimeLinkFactory = new();
    private readonly InternalRuntimeLinkFactory _internalRuntimeLinkFactory = new(queueCapacity, logger);

    [Obsolete("Use the constructor that takes the queue capacity and a logger. This one uses the default capacity and does not log dropped entries.")]
    public RuntimeLinkFactory() : this(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance) { }

    public IRuntimeLink Create(ILink link, IDictionary<Guid, IRuntimeConnector> connectedConnectors)
        => link switch
        {
            IInternalLink internalLink => _internalRuntimeLinkFactory.Create(internalLink, connectedConnectors),
            IExternalIncomingLink externalIncomingLink => _externalIncomingRuntimeLinkFactory.Create(externalIncomingLink),
            IExternalOutgoingLink externalOutgoingLink => _externalOutgoingRuntimeLinkFactory.Create(externalOutgoingLink, connectedConnectors),
            _ => throw new NotImplementedException($"Link type '{link.GetType()}' is not implemented."),
        };

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
        {
            _externalIncomingRuntimeLinkFactory.Dispose();
            _externalOutgoingRuntimeLinkFactory.Dispose();
            _internalRuntimeLinkFactory.Dispose();
        }
        _disposed = true;
    }
}
