using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class CommunicationProcessorTests
{
    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Justification = "Wird durch den Aufruf von Should().NotThrow() aufgerufen.")]
        public void CanDispose()
        {
            using var linkCache = new LinkCache([], [], () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var communicationManager = Substitute.For<ICommunicationManager>();
            var logger = Substitute.For<ILogger>();
            var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);

            Action action = processor.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            using var linkCache = new LinkCache([], Substitute.For<IEnumerable<ILink>>(), () => Substitute.For<IPoolingValidator>(),
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var communicationManager = Substitute.For<ICommunicationManager>();
            var logger = Substitute.For<ILogger>();
            var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);
            processor.Dispose();

            Action action = processor.Dispose;

            action.Should().NotThrow();
        }
    }

    public class ProcessInputCommunication
    {
        [Fact]
        public void TransfersDataToLinkIfNewer()
        {
            var input = Substitute.For<IConnector, IConnector<int>, IPoolable>();
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            var runtimeInput = Substitute.For<IRuntimeConnector, IRuntimeConnector<int>>();
            block.ProcessDataInputs.Returns([input]);
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector<int>)runtimeInput).Connector.Returns(input);
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            var externalIncomingLink1 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            var externalIncomingLink2 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            var links = new ILink[] { externalIncomingLink1, externalIncomingLink2 };
            var now = DateTime.Now;
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var expectedValidity = Validity.Valid;
            externalIncomingLink1.Id.Returns(guid1);
            externalIncomingLink2.Id.Returns(guid2);
            externalIncomingLink1.Destination.Returns(input);
            externalIncomingLink2.Destination.Returns(input);
            var runtimeLink1 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            var runtimeLink2 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            runtimeLink1.Link.Returns(externalIncomingLink1);
            runtimeLink2.Link.Returns(externalIncomingLink2);
            runtimeLink1.ChangedTime.Returns(now);
            runtimeLink2.ChangedTime.Returns(now);
            var communicationManager = Substitute.For<ICommunicationManager>();
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache([runtimeBlock], links, () => validator,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
                createRuntimeLink: (link, _) =>
            {
                if (link == externalIncomingLink1) return runtimeLink1;
                if (link == externalIncomingLink2) return runtimeLink2;
                throw new NotImplementedException();
            });
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);
            var receivedEntry1 = Substitute.For<ILinkChangedEntry, ILinkChangedEntry<int>>();
            var receivedEntry2 = Substitute.For<ILinkChangedEntry, ILinkChangedEntry<int>>();
            var expectedChangedTime = now.AddSeconds(1);
            var expectedValue1 = 42;
            var expectedValue2 = 666;
            receivedEntry1.Id.Returns(guid1);
            receivedEntry2.Id.Returns(guid2);
            receivedEntry1.Time.Returns(expectedChangedTime);
            receivedEntry2.Time.Returns(expectedChangedTime);
            receivedEntry1.Value.Returns(expectedValue1);
            receivedEntry2.Value.Returns(expectedValue2);
            receivedEntry1.Validity.Returns(expectedValidity);
            receivedEntry2.Validity.Returns(expectedValidity);
            communicationManager.ChangedEntriesReceived += Raise.Event<EventHandler<IEnumerable<ILinkChangedEntry>>>(communicationManager, new[] { receivedEntry1, receivedEntry2 });

            processor.ProcessInputCommunication();

            runtimeLink1.Received().SetData(expectedValue1, expectedChangedTime, expectedValidity);
            runtimeLink2.Received().SetData(expectedValue2, expectedChangedTime, expectedValidity);
        }

        [Fact]
        public void SortsEntriesWhenMoreThanOneEntryForQueueableDestination()
        {
            var input = Substitute.For<IConnector, IConnector<int>, IPoolable>();
            ((IPoolable)input).CrossSourcePoolingStrategy.Returns(PoolingStrategy.Queue);
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            var runtimeInput = Substitute.For<IRuntimeConnector, IRuntimeConnector<int>>();
            block.ProcessDataInputs.Returns([input]);
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector<int>)runtimeInput).Connector.Returns(input);
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            var externalIncomingLink = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            var guid = Guid.NewGuid();
            var now = DateTime.Now;
            var earlier = now.AddSeconds(-1);
            var later = now.AddSeconds(1);
            externalIncomingLink.Id.Returns(guid);
            externalIncomingLink.Destination.Returns(input);
            var runtimeLink = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            runtimeLink.Link.Returns(externalIncomingLink);
            runtimeLink.ChangedTime.Returns(earlier);
            var communicationManager = Substitute.For<ICommunicationManager>();
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache([runtimeBlock], [externalIncomingLink], () => validator,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
                createRuntimeLink: (_, _) => runtimeLink);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);
            var receivedEntryLater = Substitute.For<ILinkChangedEntry, ILinkChangedEntry<int>>();
            var receivedEntryNow = Substitute.For<ILinkChangedEntry, ILinkChangedEntry<int>>();
            var valueToReceiveFirst = 666;
            var valueToReceiveSecond = 42;
            receivedEntryLater.Id.Returns(guid);
            receivedEntryLater.Time.Returns(later);
            receivedEntryLater.Value.Returns(valueToReceiveSecond);
            receivedEntryLater.Validity.Returns(Validity.Valid);
            receivedEntryNow.Id.Returns(guid);
            receivedEntryNow.Time.Returns(now);
            receivedEntryNow.Value.Returns(valueToReceiveFirst);
            receivedEntryNow.Validity.Returns(Validity.Valid);
            communicationManager.ChangedEntriesReceived += Raise.Event<EventHandler<IEnumerable<ILinkChangedEntry>>>(communicationManager, new[] { receivedEntryLater, receivedEntryNow });

            processor.ProcessInputCommunication();

            Received.InOrder(() =>
            {
                runtimeLink.SetData(valueToReceiveFirst, now, Validity.Valid);
                runtimeLink.SetData(valueToReceiveSecond, later, Validity.Valid);
            });
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Keine reservierten Ausnahmetypen auslösen", Justification = "Ausreichend für die hier zu testende Funktionalität")]
        public void CanHandleDataTransferErrors()
        {
            var input = Substitute.For<IConnector, IConnector<int>, IPoolable>();
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            var runtimeInput = Substitute.For<IRuntimeConnector, IRuntimeConnector<int>>();
            block.ProcessDataInputs.Returns([input]);
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector<int>)runtimeInput).Connector.Returns(input);
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            var externalIncomingLink1 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            var externalIncomingLink2 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            var now = DateTime.Now;
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var expectedValidity = Validity.Valid;
            externalIncomingLink1.Id.Returns(guid1);
            externalIncomingLink2.Id.Returns(guid2);
            externalIncomingLink1.Destination.Returns(input);
            externalIncomingLink2.Destination.Returns(input);
            var runtimeLink1 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            var runtimeLink2 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            runtimeLink1.Link.Returns(externalIncomingLink1);
            runtimeLink2.Link.Returns(externalIncomingLink2);
            runtimeLink1.ChangedTime.Returns(now);
            runtimeLink2.ChangedTime.Returns(now);
            runtimeLink1
                .When(x => x.SetData(Arg.Any<object?>(), Arg.Any<DateTime>(), Arg.Any<Validity>()))
                .Throw(new Exception());
            var communicationManager = Substitute.For<ICommunicationManager>();
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache([runtimeBlock], [externalIncomingLink1, externalIncomingLink2], () => validator,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
                createRuntimeLink: (link, _) =>
            {
                if (link == externalIncomingLink1) return runtimeLink1;
                if (link == externalIncomingLink2) return runtimeLink2;
                throw new NotImplementedException();
            });
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = new TestLogger();
            using var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);
            var receivedEntry1 = Substitute.For<ILinkChangedEntry, ILinkChangedEntry<int>>();
            var receivedEntry2 = Substitute.For<ILinkChangedEntry, ILinkChangedEntry<int>>();
            var expectedChangedTime = now.AddSeconds(1);
            var expectedValue1 = 42;
            var expectedValue2 = 666;
            receivedEntry1.Id.Returns(guid1);
            receivedEntry2.Id.Returns(guid2);
            receivedEntry1.Time.Returns(expectedChangedTime);
            receivedEntry2.Time.Returns(expectedChangedTime);
            receivedEntry1.Value.Returns(expectedValue1);
            receivedEntry2.Value.Returns(expectedValue2);
            receivedEntry1.Validity.Returns(expectedValidity);
            receivedEntry2.Validity.Returns(expectedValidity);
            communicationManager.ChangedEntriesReceived += Raise.Event<EventHandler<IEnumerable<ILinkChangedEntry>>>(communicationManager, new[] { receivedEntry1, receivedEntry2 });

            processor.ProcessInputCommunication();

            runtimeLink1.Received().SetData(expectedValue1, expectedChangedTime, expectedValidity);
            runtimeLink2.Received().SetData(expectedValue2, expectedChangedTime, expectedValidity);
            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should().Contain(guid1.ToString());
        }

        [Fact]
        public void DoesNothingForReceivedEntriesWithoutLinks()
        {
            var input = Substitute.For<IConnector, IConnector<int>, IPoolable>();
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            var runtimeInput = Substitute.For<IRuntimeConnector, IRuntimeConnector<int>>();
            block.ProcessDataInputs.Returns([input]);
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector<int>)runtimeInput).Connector.Returns(input);
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            var externalIncomingLink1 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            var linkGuid = Guid.NewGuid();
            externalIncomingLink1.Id.Returns(linkGuid);
            externalIncomingLink1.Destination.Returns(input);
            var runtimeLink = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            runtimeLink.Link.Returns(externalIncomingLink1);
            var communicationManager = Substitute.For<ICommunicationManager>();
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache([runtimeBlock], [externalIncomingLink1], () => validator,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
                createRuntimeLink: (_, _) => runtimeLink);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);
            var receivedEntry = Substitute.For<ILinkChangedEntry, ILinkChangedEntry<int>>();
            var receivedGuid = Guid.NewGuid();
            receivedEntry.Id.Returns(receivedGuid);
            communicationManager.ChangedEntriesReceived += Raise.Event<EventHandler<IEnumerable<ILinkChangedEntry>>>(communicationManager, new[] { receivedEntry });

            processor.ProcessInputCommunication();

            runtimeLink.DidNotReceiveWithAnyArgs().SetData(default, default, default);
        }
    }

    public class ProcessOutputCommunication
    {
        [Fact]
        public void CallsHandleChangedOutgoingLinksWithCorrectLinks()
        {
            var output = Substitute.For<IConnector<int>>();
            var block = Substitute.For<IFunctionBlock>();
            var runtimeOutput = Substitute.For<IRuntimeConnector, IRuntimeConnector<int>>();
            runtimeOutput.Connector.Returns(output);
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            block.ProcessDataOutputs.Returns([output]);
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataOutputs.Returns([runtimeOutput]);
            var internalLink1 = Substitute.For<IInternalLink, IInternalLink<int, int, int>>();
            var internalLink2 = Substitute.For<IInternalLink, IInternalLink<int, int, int>>();
            var externalOutgoingLink1 = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<int>>();
            var externalOutgoingLink2 = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<int>>();
            internalLink1.Id.Returns(Guid.NewGuid());
            internalLink2.Id.Returns(Guid.NewGuid());
            externalOutgoingLink1.Id.Returns(Guid.NewGuid());
            externalOutgoingLink2.Id.Returns(Guid.NewGuid());
            internalLink1.Source.Returns(output);
            internalLink2.Source.Returns(output);
            externalOutgoingLink1.Source.Returns(output);
            externalOutgoingLink2.Source.Returns(output);
            var internalRuntimeLink1 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            var internalRuntimeLink2 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            var externalOutgoingRuntimeLink1 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            var externalOutgoingRuntimeLink2 = Substitute.For<IRuntimeLink, IRuntimeLink<int>>();
            internalRuntimeLink1.Link.Returns(internalLink1);
            internalRuntimeLink2.Link.Returns(internalLink2);
            externalOutgoingRuntimeLink1.Link.Returns(externalOutgoingLink1);
            externalOutgoingRuntimeLink2.Link.Returns(externalOutgoingLink2);
            ((IRuntimeLink<int>)internalRuntimeLink1).Link.Returns((ILink<int>)internalLink1);
            ((IRuntimeLink<int>)internalRuntimeLink2).Link.Returns((ILink<int>)internalLink2);
            ((IRuntimeLink<int>)externalOutgoingRuntimeLink1).Link.Returns((ILink<int>)externalOutgoingLink1);
            ((IRuntimeLink<int>)externalOutgoingRuntimeLink2).Link.Returns((ILink<int>)externalOutgoingLink2);
            internalRuntimeLink1.GetAndResetChanged().Returns(false);
            internalRuntimeLink2.GetAndResetChanged().Returns(true);
            externalOutgoingRuntimeLink1.GetAndResetChanged().Returns(true);
            externalOutgoingRuntimeLink2.GetAndResetChanged().Returns(false);
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache([runtimeBlock], [internalLink1, internalLink2, externalOutgoingLink1, externalOutgoingLink2], () => validator,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
                createRuntimeLink: (link, _) =>
            {
                if (link == internalLink1) return internalRuntimeLink1;
                if (link == internalLink2) return internalRuntimeLink2;
                if (link == externalOutgoingLink1) return externalOutgoingRuntimeLink1;
                if (link == externalOutgoingLink2) return externalOutgoingRuntimeLink2;
                throw new NotImplementedException();
            });
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var communicationManager = Substitute.For<ICommunicationManager>();
            var logger = Substitute.For<ILogger>();
            using var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();

            processor.ProcessOutputCommunication(cloneHandler);

            var expectedChangedExternalLinkIds = new[] { externalOutgoingLink1.Id };

            communicationManager
              .Received()
              .HandleChangedOutgoingLinks(
                Arg.Is<IEnumerable<ILinkChangedEntry>>(
                    entries => entries.Select(entry => entry.Id)
                    .SequenceEqualIgnoreOrder(expectedChangedExternalLinkIds)));
        }

        [Fact]
        public void CanHandleEntryCreationErrors()
        {
            var design = Substitute.For<IConnectorDesign>();
            var designName = "DummyDesign";
            var output = Substitute.For<IConnector>();
            var block = Substitute.For<IFunctionBlock>();
            var runtimeOutput = Substitute.For<IRuntimeConnector, IRuntimeConnector<int>>();
            runtimeOutput.Connector.Returns(output);
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            design.Name.Returns(designName);
            output.Design.Returns(design);
            block.ProcessDataOutputs.Returns([output]);
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataOutputs.Returns([runtimeOutput]);
            var internalLink1 = Substitute.For<IInternalLink>();
            var externalOutgoingLink1 = Substitute.For<IExternalOutgoingLink>();
            internalLink1.Id.Returns(Guid.NewGuid());
            externalOutgoingLink1.Id.Returns(Guid.NewGuid());
            internalLink1.Source.Returns(output);
            externalOutgoingLink1.Source.Returns(output);
            var internalRuntimeLink1 = Substitute.For<IRuntimeLink>();
            var externalOutgoingRuntimeLink1 = Substitute.For<IRuntimeLink>();
            internalRuntimeLink1.Link.Returns(internalLink1);
            externalOutgoingRuntimeLink1.Link.Returns(externalOutgoingLink1);
            internalRuntimeLink1.GetAndResetChanged().Returns(true);
            externalOutgoingRuntimeLink1.GetAndResetChanged().Returns(true);
            var links = new ILink[] { internalLink1, externalOutgoingLink1 };
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache([runtimeBlock], links, () => validator,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
                createRuntimeLink: (link, _) =>
            {
                if (link == internalLink1) return internalRuntimeLink1;
                if (link == externalOutgoingLink1) return externalOutgoingRuntimeLink1;
                throw new NotImplementedException();
            });
            var dataflow = Substitute.For<IDataflow>();
            dataflow.RootContainer.FunctionBlocks.Returns([block]);
            dataflow.Links.Returns(links);
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var communicationManager = Substitute.For<ICommunicationManager>();
            var logger = new TestLogger();
            using var processor = new CommunicationProcessor(linkCache, nameCache, communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, logger);
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();

            processor.ProcessOutputCommunication(cloneHandler);

            communicationManager
                .DidNotReceiveWithAnyArgs()
                .HandleChangedOutgoingLinks([]);
            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages
                .Where(x =>
                    x.Contains("external output link", StringComparison.OrdinalIgnoreCase) &&
                    x.Contains(designName, StringComparison.OrdinalIgnoreCase))
                .Should().HaveCount(1);
        }
    }
}
