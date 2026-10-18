using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class LinkCacheTests
{
    private protected IEnumerable<ILink> AllLinks { get; }
    private protected IExternalIncomingLink ExternalIncomingLink { get; }
    private protected Guid ExternalIncomingLinkId { get; }
    private protected IExternalOutgoingLink ExternalOutgoingLink1 { get; }
    private protected Guid ExternalOutgoingLink1Id { get; }
    private protected IExternalOutgoingLink ExternalOutgoingLink2 { get; }
    private protected Guid ExternalOutgoingLink2Id { get; }
    private protected IConnector Input1 { get; }
    private protected Guid Input1Id { get; }
    private protected IConnector Input2 { get; }
    private protected Guid Input2Id { get; }
    private protected IInternalLink InternalLink1 { get; }
    private protected Guid InternalLink1Id { get; }
    private protected IInternalLink InternalLink2 { get; }
    private protected Guid InternalLink2Id { get; }
    private protected IConnector Output { get; }
    private protected IRuntimeFunctionBlock[] RuntimeFunctionBlocks { get; }
    private protected IRuntimeConnector RuntimeInput1 { get; }
    private protected IRuntimeConnector RuntimeInput2 { get; }
    private protected IRuntimeConnector RuntimeOutput { get; }

    public LinkCacheTests()
    {
        Input1 = Substitute.For<IConnector, IConnector<object>, IPoolable>();
        Input2 = Substitute.For<IConnector, IConnector<object>, IPoolable>();
        Input1Id = Guid.NewGuid();
        Input2Id = Guid.NewGuid();
        Input1.Id.Returns(Input1Id);
        Input2.Id.Returns(Input2Id);
        ((IPoolable)Input1).CrossSourcePoolingStrategy.Returns(PoolingStrategy.Queue);
        ((IPoolable)Input2).CrossSourcePoolingStrategy.Returns(PoolingStrategy.Aggregate);
        ((IPoolable)Input2).CrossSourceAggregatingPooling.Returns(Substitute.For<IAggregatingPooling<object>>());
        Output = Substitute.For<IConnector, IConnector<object>>();
        RuntimeInput1 = Substitute.For<IRuntimeConnector, IRuntimeConnector<object>>();
        RuntimeInput2 = Substitute.For<IRuntimeConnector, IRuntimeConnector<object>>();
        RuntimeOutput = Substitute.For<IRuntimeConnector, IRuntimeConnector<object>>();
        RuntimeInput1.Connector.Returns(Input1);
        RuntimeInput2.Connector.Returns(Input2);
        RuntimeOutput.Connector.Returns(Output);
        ((IRuntimeConnector<object>)RuntimeInput1).Connector.Returns(Input1);
        ((IRuntimeConnector<object>)RuntimeInput2).Connector.Returns(Input2);
        ((IRuntimeConnector<object>)RuntimeOutput).Connector.Returns(Output);
        var block = Substitute.For<IFunctionBlock>();
        block.ProcessDataInputs.Returns([Input1, Input2]);
        block.ProcessDataOutputs.Returns([Output]);
        var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
        runtimeBlock.FunctionBlock.Returns(block);
        runtimeBlock.ProcessDataInputs.Returns([RuntimeInput1, RuntimeInput2]);
        runtimeBlock.ProcessDataOutputs.Returns([RuntimeOutput]);

        ExternalIncomingLink = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<object, object, object>>();
        ExternalIncomingLinkId = Guid.NewGuid();
        ExternalOutgoingLink1 = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<object>>();
        ExternalOutgoingLink1Id = Guid.NewGuid();
        ExternalOutgoingLink2 = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<object>>();
        ExternalOutgoingLink2Id = Guid.NewGuid();
        InternalLink1 = Substitute.For<IInternalLink, IInternalLink<object, object, object>>();
        InternalLink1Id = Guid.NewGuid();
        InternalLink2 = Substitute.For<IInternalLink, IInternalLink<object, object, object>>();
        InternalLink2Id = Guid.NewGuid();

        ExternalIncomingLink.Destination.Returns(Input1);
        ExternalIncomingLink.Id.Returns(ExternalIncomingLinkId);
        ExternalIncomingLink.TypeConversion.Returns((ITypeConversion?)null);
        ExternalOutgoingLink1.Source.Returns(Output);
        ((IExternalOutgoingLink<object>)ExternalOutgoingLink1).Source.Returns(Output);
        ExternalOutgoingLink1.Id.Returns(ExternalOutgoingLink1Id);
        ExternalOutgoingLink1.TypeConversion.Returns((ITypeConversion?)null);
        ExternalOutgoingLink2.Source.Returns(Input1);
        ((IExternalOutgoingLink<object>)ExternalOutgoingLink2).Source.Returns(Input1);
        ExternalOutgoingLink2.Id.Returns(ExternalOutgoingLink2Id);
        ExternalOutgoingLink2.TypeConversion.Returns((ITypeConversion?)null);
        InternalLink1.Id.Returns(InternalLink1Id);
        InternalLink1.Source.Returns(Output);
        ((IInternalLink<object, object, object>)InternalLink1).Source.Returns(Output);
        InternalLink1.Destination.Returns(Input1);
        InternalLink1.TypeConversion.Returns((ITypeConversion?)null);
        InternalLink2.Id.Returns(InternalLink2Id);
        InternalLink2.Source.Returns(Output);
        ((IInternalLink<object, object, object>)InternalLink2).Source.Returns(Output);
        InternalLink2.Destination.Returns(Input2);
        InternalLink2.TypeConversion.Returns((ITypeConversion?)null);

        RuntimeFunctionBlocks = [runtimeBlock];
        AllLinks =
        [
            ExternalIncomingLink,
            ExternalOutgoingLink1,
            ExternalOutgoingLink2,
            InternalLink1,
            InternalLink2,
        ];
    }

    public class Dispose : LinkCacheTests
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var linkCache = new LinkCache([], [], () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);

            Action action = linkCache.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var linkCache = new LinkCache([], [], () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            linkCache.Dispose();

            Action action = linkCache.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void DisposesIDisposableIncomingInputLinkGroups()
        {
            var disposableGroup = Substitute.For<IIncomingInputLinkGroup, IDisposable>();
            var linkCache = new LinkCache(RuntimeFunctionBlocks, AllLinks, () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
                createIncomingInputLinkGroup: (_, _, _) => disposableGroup);

            linkCache.Dispose();

            ((IDisposable)disposableGroup).Received().Dispose();
        }
    }

    public class GetIncomingInputLinkGroups : LinkCacheTests
    {
        [Fact]
        public void GetsIncomingInputLinkGroups()
        {
            var validator = Substitute.For<IPoolingValidator>();
            var getValidator = new Func<IPoolingValidator>(() => validator);
            using var linkCache = new LinkCache(RuntimeFunctionBlocks, AllLinks, getValidator,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            var externalIncomingRuntimeLink = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == ExternalIncomingLink);
            var internalRuntimeLink1 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == InternalLink1);
            var internalRuntimeLink2 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == InternalLink2);
            using var expectedQueueingGroup = new QueueingIncomingInputLinkGroup<object>(
                    connector: (IRuntimeConnector<object>)RuntimeInput1,
                    links:
                    [
                        externalIncomingRuntimeLink,
                        internalRuntimeLink1,
                    ],
                    getPoolingValidator: getValidator,
                    EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, 
                    NullLogger.Instance);
            var expectedAggregatingGroup = new AggregatingIncomingInputLinkGroup<object>(
                    connector: (IRuntimeConnector<object>)RuntimeInput2,
                    links:
                    [
                        internalRuntimeLink2,
                    ]);
            IIncomingInputLinkGroup[] expected = [expectedQueueingGroup, expectedAggregatingGroup,];

            var result = linkCache.GetIncomingInputLinkGroups(RuntimeFunctionBlocks.First());

            result.Should().BeEquivalentTo(expected);
        }
    }

    public class GetOutgoingInputLinkGroups : LinkCacheTests
    {
        [Fact]
        public void GetsOutgoingInputLinkGroups()
        {
            using var linkCache = new LinkCache(RuntimeFunctionBlocks, AllLinks, () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            var externalOutgoingRuntimeLink1 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == ExternalOutgoingLink1);
            var externalOutgoingRuntimeLink2 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == ExternalOutgoingLink2);
            var internalRuntimeLink1 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == InternalLink1);
            var internalRuntimeLink2 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == InternalLink2);

            var internalLink1DestinationLinkGroup = linkCache.GetIncomingInputLinkGroups(RuntimeFunctionBlocks.First()).First(x => x.Connector.Connector.Id == Input1Id);
            var internalLink2DestinationLinkGroup = linkCache.GetIncomingInputLinkGroups(RuntimeFunctionBlocks.First()).First(x => x.Connector.Connector.Id == Input2Id);
            IOutgoingInputLinkGroup[] expected =
            [
                new OutgoingInputLinkGroup<object>(
                    connector: (IRuntimeConnector<object>)RuntimeInput1,
                    links: [externalOutgoingRuntimeLink2]),
                new OutgoingInputLinkGroup<object>(
                    connector: (IRuntimeConnector<object>)RuntimeInput2,
                    links: []),
            ];

            var result = linkCache.GetOutgoingInputLinkGroups(RuntimeFunctionBlocks.First());

            result.Should().BeEquivalentTo(expected);
        }
    }

    public class GetOutputLinkGroups : LinkCacheTests
    {
        [Fact]
        public void GetsOutputLinkGroups()
        {
            using var linkCache = new LinkCache(RuntimeFunctionBlocks, AllLinks, () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            var externalOutgoingRuntimeLink1 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == ExternalOutgoingLink1);
            var externalOutgoingRuntimeLink2 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == ExternalOutgoingLink2);
            var internalRuntimeLink1 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == InternalLink1);
            var internalRuntimeLink2 = (IRuntimeLink<object>)linkCache.Links.First(x => x.Link == InternalLink2);

            var internalLink1DestinationLinkGroup = linkCache.GetIncomingInputLinkGroups(RuntimeFunctionBlocks.First()).First(x => x.Connector.Connector.Id == Input1Id);
            var internalLink2DestinationLinkGroup = linkCache.GetIncomingInputLinkGroups(RuntimeFunctionBlocks.First()).First(x => x.Connector.Connector.Id == Input2Id);
            IOutputLinkGroup[] expected =
            [
                new OutputLinkGroup<object>(
                    connector: (IRuntimeConnector<object>)RuntimeOutput,
                    links:
                    [
                        externalOutgoingRuntimeLink1,
                        internalRuntimeLink1,
                        internalRuntimeLink2,
                    ]
                )
            ];

            var result = linkCache.GetOutputLinkGroups(RuntimeFunctionBlocks.First());

            result.Should().BeEquivalentTo(expected);
        }
    }

    public class GetExternalIncomingLinks : LinkCacheTests
    {
        [Fact]
        public void GetsExternalIncomingLinks()
        {
            using var linkCache = new LinkCache(RuntimeFunctionBlocks, AllLinks, () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            var externalIncomingRuntimeLink = linkCache.Links.First(x => x.Link == ExternalIncomingLink);
            var expected = new KeyValuePair<Guid, IRuntimeLink>(ExternalIncomingLinkId, externalIncomingRuntimeLink);

            var result = linkCache.GetExternalIncomingLinks();

            result.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeEquivalentTo(expected);
        }
    }

    public class GetExternalOutgoingLinks : LinkCacheTests
    {
        [Fact]
        public void GetsExternalOutgoingLinks()
        {
            using var linkCache = new LinkCache(RuntimeFunctionBlocks, AllLinks, () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            var expected = linkCache.Links.Where(x => x.Link == ExternalOutgoingLink1 || x.Link == ExternalOutgoingLink2);

            var result = linkCache.GetExternalOutgoingLinks();

            result.Should().BeEquivalentTo(expected);
        }
    }
}
