using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class OutgoingInputLinkGroupTests
{
    public class Constructor
    {
        [Fact]
        public void StoresConnector()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new OutgoingInputLinkGroup<object>(connector, links);

            group.Connector.Should().BeSameAs(connector);
        }

        [Fact]
        public void StoresLinks()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new OutgoingInputLinkGroup<object>(connector, links);

            group.Links.Should().BeSameAs(links);
        }
    }

    public class TransferValueToLinks
    {
        [Fact]
        public void CallsBaseWithCorrectValues()
        {
            var time = DateTime.Now;
            var validity = Validity.Valid;
            var value = 42;
            var connector = Substitute.For<IRuntimeConnector<int>>();
            connector.ChangedTime.Returns(time);
            connector.Value.Returns(value);
            connector.Validity.Returns(validity);
            IRuntimeLink[] links =
            [
                Substitute.For<IRuntimeLink>(),
                Substitute.For<IRuntimeLink>(),
            ];
            var group = new OutgoingInputLinkGroup<int>(connector, links);

            group.TransferValueToLinks(default);

            foreach (var link in links)
                link.Received().SetData(value, time, validity);
        }
    }
}
