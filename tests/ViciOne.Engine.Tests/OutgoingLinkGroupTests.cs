using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class OutgoingLinkGroupTests
{
    public class Constructor
    {
        [Fact]
        public void StoresConnector()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new MockOutgoingLinkGroup<object>(connector, links);

            group.Connector.Should().BeSameAs(connector);
        }

        [Fact]
        public void StoresLinks()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new MockOutgoingLinkGroup<object>(connector, links);

            group.Links.Should().BeSameAs(links);
        }
    }

    public class TransferValueToLinks
    {
        private MockOutgoingLinkGroup<int> Group { get; }
        private IRuntimeLink[] Links { get; }
        private DateTime Time { get; }
        private Validity Validity { get; }

        public TransferValueToLinks()
        {
            Time = DateTime.Now;
            Validity = Validity.Valid;
            var connector = Substitute.For<IRuntimeConnector<int>>();
            connector.ChangedTime.Returns(Time);
            connector.Value.Returns(666);
            connector.Validity.Returns(Validity);
            Links =
            [
                Substitute.For<IRuntimeLink>(),
                Substitute.For<IRuntimeLink>(),
            ];
            Group = new MockOutgoingLinkGroup<int>(connector, Links);
        }

        [Fact]
        public void CallsSetDataWithCorrectValues()
        {
            var expectedValue = 42;

            Group.TransferValueToLinks(null, expectedValue);

            foreach (var link in Links)
                link.Received().SetData(expectedValue, Time, Validity);
        }

        [Fact]
        public void PassesValueToCloneHandler()
        {
            var expectedValue = 42;
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();

            Group.TransferValueToLinks(cloneHandler, expectedValue);

            cloneHandler.Received().CloneIfNecessary(expectedValue);
        }

        [Fact]
        public void UsesValueFromCloneHandler()
        {
            var expectedValue = 4711;
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();
            cloneHandler.CloneIfNecessary<int>(default).ReturnsForAnyArgs(expectedValue);

            Group.TransferValueToLinks(cloneHandler, 42);

            foreach (var link in Links)
                link.Received().SetData(expectedValue, Time, Validity);
        }
    }

    private sealed class MockOutgoingLinkGroup<TValue>(IRuntimeConnector<TValue> connector, IRuntimeLink[] links) :
        OutgoingLinkGroup<TValue>(connector, links)
    {
        public new void TransferValueToLinks(IReferenceTypeCloneHandler? cloneHandler, TValue? value)
            => base.TransferValueToLinks(cloneHandler, value);
    }
}
