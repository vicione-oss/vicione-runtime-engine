using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine.Benchmarks;

internal sealed class StubIncomingInputLinkGroup(IRuntimeConnector connector, IEnumerable<IRuntimeLink> links) : IIncomingInputLinkGroup
{
    public IRuntimeConnector Connector { get; } = connector;
    public IEnumerable<IRuntimeLink> Links { get; } = links;
    public bool NeedsFurtherProcessing => false;

    public void Pool(IPoolingValidator validator, IConnectorValueTransferInterceptor interceptor)
        => throw new NotSupportedException();
}
