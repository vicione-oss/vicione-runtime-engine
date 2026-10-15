using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class CommunicationReceivedEntriesObserverTests
{
    public class Constructor
    {
        [Fact]
        public void AttachesToChangedEntriesReceived()
        {
            var communicationManager = Substitute.For<ICommunicationManager>();

            using var observer = new CommunicationReceivedEntriesObserver(communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, NullLogger.Instance);

            communicationManager.ReceivedWithAnyArgs(1).ChangedEntriesReceived += null;
        }

        [Fact]
        public void AttachedEventHandlerStoresOnlyEntriesUntilCapacityReached()
        {
            const int BufferCapacity = 100;

            var entriesToReceive = Enumerable.Range(0, BufferCapacity * 2).Select(_ => Substitute.For<ILinkChangedEntry>()).ToArray();
            var expectedEntries = entriesToReceive.Take(BufferCapacity).ToArray();
            var communicationManager = Substitute.For<ICommunicationManager>();
            using var observer = new CommunicationReceivedEntriesObserver(communicationManager, BufferCapacity, NullLogger.Instance);

            communicationManager.ChangedEntriesReceived += Raise.Event<EventHandler<IEnumerable<ILinkChangedEntry>>>(communicationManager, entriesToReceive);

            var receivedEntries = observer.GetAndResetReceivedEntries();
            receivedEntries.Should().BeEquivalentTo(expectedEntries);
        }
    }

    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var communicationManager = Substitute.For<ICommunicationManager>();
            var observer = new CommunicationReceivedEntriesObserver(communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, NullLogger.Instance);

            Action action = observer.Dispose;

            action.Should().NotThrow();
            communicationManager.ReceivedWithAnyArgs(1).ChangedEntriesReceived -= null;
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var communicationManager = Substitute.For<ICommunicationManager>();
            var observer = new CommunicationReceivedEntriesObserver(communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, NullLogger.Instance);
            observer.Dispose();

            Action action = observer.Dispose;

            action.Should().NotThrow();
            communicationManager.ReceivedWithAnyArgs(1).ChangedEntriesReceived -= null;
        }
    }

    public class GetAndResetReceivedEntries
    {
        public static TheoryData<IEnumerable<IEnumerable<ILinkChangedEntry>>, IEnumerable<ILinkChangedEntry>> ArgumentsFor_GetsAndResetsReceivedEntries
        {
            get
            {
                var entry11 = Substitute.For<ILinkChangedEntry>();
                var entry21 = Substitute.For<ILinkChangedEntry>();
                var entry31 = Substitute.For<ILinkChangedEntry>();
                var entry32 = Substitute.For<ILinkChangedEntry>();
                var entry41 = Substitute.For<ILinkChangedEntry>();
                var entry42 = Substitute.For<ILinkChangedEntry>();
                var entry43 = Substitute.For<ILinkChangedEntry>();

                return new()
                {
                    { [], [] },
                    { new[] { new[] { entry11 } }, new[] { entry11 } },
                    { new[] { [entry21], Array.Empty<ILinkChangedEntry>() }, new[] { entry21 } },
                    { new[] { [entry31], new[] { entry32 } }, new[] { entry31, entry32 } },
                    { new[] { [entry41], new[] { entry42, entry43 } }, new[] { entry41, entry42, entry43 } }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsAndResetsReceivedEntries))]
        public void GetsAndResetsReceivedEntries(IEnumerable<IEnumerable<ILinkChangedEntry>> entriesToReceive,
            IEnumerable<ILinkChangedEntry> expectedReceivedEntries)
        {
            var communicationManager = Substitute.For<ICommunicationManager>();
            using var observer = new CommunicationReceivedEntriesObserver(communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, NullLogger.Instance);
            foreach (var entries in entriesToReceive)
                communicationManager.ChangedEntriesReceived += Raise.Event<EventHandler<IEnumerable<ILinkChangedEntry>>>(communicationManager, entries);

            var receivedEntries1 = observer.GetAndResetReceivedEntries();
            var receivedEntries2 = observer.GetAndResetReceivedEntries();

            receivedEntries1.Should().BeEquivalentTo(expectedReceivedEntries);
            receivedEntries2.Should().BeEmpty();
        }

        [Fact]
        public void LogsWarningOnReceiveBufferOverflow()
        {
            var communicationManager = Substitute.For<ICommunicationManager>();
            var logger = new TestLogger();
            var capacity = 5;
            var expectedDropped = 3;
            using var observer = new CommunicationReceivedEntriesObserver(communicationManager, capacity, logger);
            var entriesToReceive = Enumerable.Range(0, capacity + expectedDropped).Select(_ => Substitute.For<ILinkChangedEntry>()).ToArray();
            communicationManager.ChangedEntriesReceived += Raise.Event<EventHandler<IEnumerable<ILinkChangedEntry>>>(communicationManager, entriesToReceive);

            _ = observer.GetAndResetReceivedEntries();

            var warnings = logger.GetLoggedMessages(LogLevel.Warning);
            warnings.Should().HaveCount(1);
            warnings.First().Should().MatchEquivalentOf($"*overflow*drop*{expectedDropped}*capacity*{capacity}*{nameof(EngineQueueOptions.CommunicationReceiveBufferCapacity)}*");
        }

        [Fact]
        public void ReturnsArrayEmptySingletonIfNoEntriesReceived()
        {
            var communicationManager = Substitute.For<ICommunicationManager>();
            using var observer = new CommunicationReceivedEntriesObserver(communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, NullLogger.Instance);

            var receivedEntries = observer.GetAndResetReceivedEntries();

            receivedEntries.Should().BeSameAs(Array.Empty<ILinkChangedEntry>());
        }
    }
}
