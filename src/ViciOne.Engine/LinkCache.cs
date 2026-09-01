using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Contracts.Runtime.Extensions;

namespace ViciOne.Engine;

internal sealed class LinkCache : IDisposable
{
    private bool _disposed;
    private readonly Dictionary<Guid, IRuntimeLink> _externalIncomingLinks;
    private readonly List<IRuntimeLink> _externalOutgoingLinks;
    private readonly Dictionary<IRuntimeFunctionBlock, IIncomingInputLinkGroup[]> _incomingInputLinkGroups;
    private readonly Dictionary<IRuntimeFunctionBlock, IOutgoingInputLinkGroup[]> _outgoingInputLinkGroups;
    private readonly Dictionary<IRuntimeFunctionBlock, IOutputLinkGroup[]> _outputLinkGroups;

    // TODO: this property is only useful for the tests, maybe it can still be eliminated
    public IRuntimeLink[] Links { get; }

    public LinkCache(IRuntimeFunctionBlock[] runtimeFunctionBlocks, IEnumerable<ILink> allLinks, Func<IPoolingValidator> getPoolingValidator,
        int perLinkQueueCapacity, int crossSourceQueueCapacityPerLink, ILogger logger,
        Func<IRuntimeConnector, IEnumerable<IRuntimeLink>, Func<IPoolingValidator>, IIncomingInputLinkGroup>? createIncomingInputLinkGroup = null,
        Func<ILink, IDictionary<Guid, IRuntimeConnector>, IRuntimeLink>? createRuntimeLink = null)
    {
        using IncomingInputLinkGroupFactory incomingInputLinkGroupFactory = new(crossSourceQueueCapacityPerLink, logger);
        using OutgoingInputLinkGroupFactory outgoingInputLinkGroupFactory = new();
        using OutputLinkGroupFactory outputLinkGroupFactory = new();
        using RuntimeLinkFactory runtimeLinkFactory = new(perLinkQueueCapacity, logger);

        createIncomingInputLinkGroup ??= incomingInputLinkGroupFactory.Create;
        createRuntimeLink ??= runtimeLinkFactory.Create;

        IEnumerable<(ILink, Dictionary<Guid, IRuntimeConnector>)> relevantLinks = GetRelevantLinks(runtimeFunctionBlocks, allLinks);
        Links = CreateRuntimeLinks(relevantLinks, createRuntimeLink);

        _externalIncomingLinks = CreateExternalIncomingLinkCache(Links);
        _externalOutgoingLinks = CreateExternalOutgoingLinkCache(Links);
        _incomingInputLinkGroups = CreateIncomingInputLinkCache(runtimeFunctionBlocks, Links, getPoolingValidator, createIncomingInputLinkGroup);
        _outgoingInputLinkGroups = CreateOutgoingInputLinkCache(runtimeFunctionBlocks, Links, outgoingInputLinkGroupFactory);
        _outputLinkGroups = CreateOutputLinkCache(runtimeFunctionBlocks, Links, outputLinkGroupFactory);
    }

    private static Dictionary<Guid, IRuntimeLink> CreateExternalIncomingLinkCache(IRuntimeLink[] links)
        => links
            .Where(x => x.Link is IExternalIncomingLink)
            .ToDictionary(
                x => x.Link.Id,
                x => x);

    private static List<IRuntimeLink> CreateExternalOutgoingLinkCache(IRuntimeLink[] links)
        => [.. links.Where(x => x.Link is IExternalOutgoingLink)];

    private static Dictionary<IRuntimeFunctionBlock, IIncomingInputLinkGroup[]> CreateIncomingInputLinkCache(
        IRuntimeFunctionBlock[] runtimeFunctionBlocks, IRuntimeLink[] links, Func<IPoolingValidator> getPoolingValidator,
        Func<IRuntimeConnector, IEnumerable<IRuntimeLink>, Func<IPoolingValidator>, IIncomingInputLinkGroup> createIncomingInputLinkGroup)
        => runtimeFunctionBlocks
            .ToDictionary(
                x => x,
                x => x.GetInputs()
                    .Select(input =>
                        createIncomingInputLinkGroup(
                            input,
                            links.Where(x => x.Link is ILinkWithDestination linkWithDestination && linkWithDestination.Destination.Id == input.Connector.Id),
                            getPoolingValidator))
                    .ToArray());

    private static Dictionary<IRuntimeFunctionBlock, IOutgoingInputLinkGroup[]> CreateOutgoingInputLinkCache(
        IRuntimeFunctionBlock[] runtimeFunctionBlocks, IRuntimeLink[] links, OutgoingInputLinkGroupFactory outgoingInputLinkGroupFactory)
        => runtimeFunctionBlocks
            .ToDictionary(
                x => x,
                x => x.GetInputs()
                    .Select(input =>
                        outgoingInputLinkGroupFactory.Create(
                            input,
                            links.Where(x => x.Link is ILinkWithSource linkWithSource && linkWithSource.Source.Id == input.Connector.Id)))
                    .ToArray());

    private static Dictionary<IRuntimeFunctionBlock, IOutputLinkGroup[]> CreateOutputLinkCache(
        IRuntimeFunctionBlock[] runtimeFunctionBlocks, IRuntimeLink[] links, OutputLinkGroupFactory outputLinkGroupFactory)
        => runtimeFunctionBlocks
            .ToDictionary(
                x => x,
                x => x.GetOutputs()
                    .Select(output =>
                        outputLinkGroupFactory.Create(
                            output,
                            links.Where(x => x.Link is ILinkWithSource linkWithSource && linkWithSource.Source.Id == output.Connector.Id)))
                    .ToArray());

    private static IRuntimeLink[] CreateRuntimeLinks(IEnumerable<(ILink Link, Dictionary<Guid, IRuntimeConnector> ConnectedConnectors)> links,
        Func<ILink, IDictionary<Guid, IRuntimeConnector>, IRuntimeLink> createRuntimeLink)
        => [.. links.Select(link => createRuntimeLink(link.Link, link.ConnectedConnectors))];

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            foreach (IIncomingInputLinkGroup[] groups in _incomingInputLinkGroups.Values)
                foreach (IIncomingInputLinkGroup group in groups)
                    if (group is IDisposable disposable)
                        disposable.Dispose();

            _incomingInputLinkGroups.Clear();
            _outgoingInputLinkGroups.Clear();
            _outputLinkGroups.Clear();
            _externalIncomingLinks.Clear();
            _externalOutgoingLinks.Clear();
        }

        _disposed = true;
    }

    public IDictionary<Guid, IRuntimeLink> GetExternalIncomingLinks()
        => _externalIncomingLinks;

    public IEnumerable<IRuntimeLink> GetExternalOutgoingLinks()
        => _externalOutgoingLinks;

    public IEnumerable<IIncomingInputLinkGroup> GetIncomingInputLinkGroups(IRuntimeFunctionBlock runtimeFunctionBlock)
        => _incomingInputLinkGroups[runtimeFunctionBlock];

    public IEnumerable<IOutgoingInputLinkGroup> GetOutgoingInputLinkGroups(IRuntimeFunctionBlock runtimeFunctionBlock)
        => _outgoingInputLinkGroups[runtimeFunctionBlock];

    public IEnumerable<IOutputLinkGroup> GetOutputLinkGroups(IRuntimeFunctionBlock runtimeFunctionBlock)
        => _outputLinkGroups[runtimeFunctionBlock];

    private static IEnumerable<(ILink, Dictionary<Guid, IRuntimeConnector>)> GetRelevantLinks(IRuntimeFunctionBlock[] runtimeFunctionBlocks, IEnumerable<ILink> links)
    {
        var inputs = runtimeFunctionBlocks
            .SelectMany(x => x.GetInputs())
            .ToDictionary(x => x.Connector.Id, x => x);
        var outputs = runtimeFunctionBlocks
            .SelectMany(x => x.GetOutputs())
            .ToDictionary(x => x.Connector.Id, x => x);

        foreach (ILink link in links)
        {
            bool isRelevant = false;
            IRuntimeConnector? sourceRuntimeConnector = null;
            IRuntimeConnector? destinationRuntimeConnector = null;
            isRelevant |= link is ILinkWithSource linkWithSource && (outputs.TryGetValue(linkWithSource.Source.Id, out sourceRuntimeConnector) || inputs.TryGetValue(linkWithSource.Source.Id, out sourceRuntimeConnector));
            isRelevant |= link is ILinkWithDestination linkWithDestination && inputs.TryGetValue(linkWithDestination.Destination.Id, out destinationRuntimeConnector);

            if (isRelevant)
            {
                var relevantConnectors = new Dictionary<Guid, IRuntimeConnector>();
                if (sourceRuntimeConnector is not null)
                    relevantConnectors.Add(sourceRuntimeConnector.Connector.Id, sourceRuntimeConnector);
                if (destinationRuntimeConnector is not null)
                    relevantConnectors.Add(destinationRuntimeConnector.Connector.Id, destinationRuntimeConnector);
                yield return (link, relevantConnectors);
            }
        }
    }
}
