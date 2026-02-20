using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class InMemoryCommunicationManagerTests
{
    public class Dispose
    {
        [Fact]
        public void CanDispose()
        {
            using var manager = new InMemoryCommunicationManager();

            Action action = manager.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            using var manager = new InMemoryCommunicationManager();

            Action action = () =>
            {
                manager.Dispose();
                manager.Dispose();
            };

            action.Should().NotThrow();
        }
    }

    public class GetAndResetHandledExternalOutgoingLinkChangedEntries
    {
        [Fact]
        public void GetsChangedConnectorEntries()
        {
            using var manager = new InMemoryCommunicationManager();
            var changedEntries = CreateChangedEntries();
            manager.HandleChangedOutgoingLinks(changedEntries);

            var returned = manager.GetAndResetHandledExternalOutgoingLinkChangedEntries();

            returned.Should().ContainAllButNotMore(changedEntries);
        }

        [Fact]
        public void ResetsChangedConnectorEntries()
        {
            using var manager = new InMemoryCommunicationManager();
            var changedEntries = CreateChangedEntries();
            manager.HandleChangedOutgoingLinks(changedEntries);
            _ = manager.GetAndResetHandledExternalOutgoingLinkChangedEntries();

            var returned = manager.GetAndResetHandledExternalOutgoingLinkChangedEntries();

            returned.Should().BeEmpty();
        }
    }

    public class HandleChangedOutgoingLinks
    {
        [Fact]
        public void StoresSpecifiedEntries()
        {
            using var manager = new InMemoryCommunicationManager();
            var changedEntries = CreateChangedEntries();

            manager.HandleChangedOutgoingLinks(changedEntries);

            manager.GetAndResetHandledExternalOutgoingLinkChangedEntries().Should().ContainAllButNotMore(changedEntries);
        }

        [Fact]
        public void RaisesChangedConnectorEntriesChanged()
        {
            using var manager = new InMemoryCommunicationManager();
            var changedEntries = CreateChangedEntries();
            using var monitoredManager = manager.Monitor();

            manager.HandleChangedOutgoingLinks(changedEntries);

            monitoredManager.Should()
                .Raise(nameof(InMemoryCommunicationManager.HandledExternalOutgoingLinkChangedEntriesChanged))
                .WithSender(manager);
        }

        [Fact]
        public void DoesNothingIfNoEntriesSpecified()
        {
            using var manager = new InMemoryCommunicationManager();
            using var monitoredManager = manager.Monitor();

            manager.HandleChangedOutgoingLinks([]);

            manager.GetAndResetHandledExternalOutgoingLinkChangedEntries().Should().BeEmpty();
            monitoredManager.Should().NotRaise(nameof(InMemoryCommunicationManager.HandledExternalOutgoingLinkChangedEntriesChanged));
        }
    }

    public class RaiseChangedEntriesReceived
    {
        [Fact]
        public void RaisesChangedEntriesReceived()
        {
            using var manager = new InMemoryCommunicationManager();
            var changedEntries = CreateChangedEntries();
            using var monitoredManager = manager.Monitor();

            manager.RaiseChangedEntriesReceived(changedEntries);

            monitoredManager.Should()
                .Raise(nameof(InMemoryCommunicationManager.ChangedEntriesReceived))
                .WithSender(manager)
                .WithArgs<IEnumerable<ILinkChangedEntry>>(args => args.SequenceEqual(changedEntries));
        }
    }

    private static ChangedEntry[] CreateChangedEntries()
        =>
        [
            new() { Time = DateTime.UtcNow.AddSeconds(1), Id = Guid.Parse("895d9e45-416e-448f-9cfd-d19dafe49847"), Validity = Validity.Valid, Value = 42, },
            new() { Time = DateTime.UtcNow.AddSeconds(2), Id = Guid.Parse("71d434a2-ebc7-409f-92c8-62f0313d6958"), Validity = Validity.Valid, Value = 66.6, },
            new() { Time = DateTime.UtcNow.AddSeconds(3), Id = Guid.Parse("5551e064-ee75-42a2-9d0c-17787ab5c058"), Validity = Validity.Invalid, Value = "Test", },
            new() { Time = DateTime.UtcNow.AddSeconds(4), Id = Guid.Parse("eeedd9f4-50b7-4d39-a859-dbd1e986653f"), Validity = Validity.Invalid, Value = true, },
        ];
}
