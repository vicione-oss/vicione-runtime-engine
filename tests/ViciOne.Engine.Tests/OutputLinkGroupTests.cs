using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class OutputLinkGroupTests
{
    public class Constructor
    {
        [Fact]
        public void StoresConnector()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new OutputLinkGroup<object>(connector, links);

            group.Connector.Should().BeSameAs(connector);
        }

        [Fact]
        public void StoresLinks()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new OutputLinkGroup<object>(connector, links);

            group.Links.Should().BeSameAs(links);
        }
    }

    public class TransferValueToLinks
    {
        private Guid ConnectorGuid { get; }
        private OutputLinkGroup<int> Group { get; }
        private DefaultConnectorValueTransferInterceptor Interceptor { get; }
        private IRuntimeLink[] Links { get; }
        private DateTime Time { get; }
        private Validity Validity { get; }
        private int Value { get; }

        public TransferValueToLinks()
        {
            Time = DateTime.Now;
            Validity = Validity.Valid;
            Value = 42;
            ConnectorGuid = Guid.NewGuid();
            var connector = Substitute.For<IRuntimeConnector<int>>();
            connector.Connector.Id.Returns(ConnectorGuid);
            connector.ChangedTime.Returns(Time);
            connector.Value.Returns(Value);
            connector.Validity.Returns(Validity);
            Links =
            [
                Substitute.For<IRuntimeLink>(),
                Substitute.For<IRuntimeLink>(),
            ];
            Group = new OutputLinkGroup<int>(connector, Links);
            Interceptor = new DefaultConnectorValueTransferInterceptor();
        }

        [Fact]
        public void CallsBaseWithCorrectValues()
        {
            Group.TransferValueToLinks(null, Interceptor);

            foreach (var link in Links)
                link.Received().SetData(Value, Time, Validity);
        }

        [Fact]
        public void PassesConnectorValueToInterceptor()
        {
            int passedInterceptorArg = default;
            Interceptor.RegisterInterceptor<int>(ConnectorGuid, (arg) =>
            {
                passedInterceptorArg = arg;
                return new DefaultConnectorValueTransferInterceptionResult<int>() { Value = arg };
            });

            Group.TransferValueToLinks(default, Interceptor);

            passedInterceptorArg.Should().Be(Value);
        }

        [Fact]
        public void UsesValueFromInterceptor()
        {
            var expectedValue = 4711;
            Interceptor.RegisterInterceptor<int>(ConnectorGuid, (_) => new DefaultConnectorValueTransferInterceptionResult<int>() { Value = expectedValue });

            Group.TransferValueToLinks(default, Interceptor);

            foreach (var link in Links)
                link.Received().SetData(expectedValue, Time, Validity);
        }

        [Fact]
        public void DoesNothingIfInterceptorCancelsTransfer()
        {
            Interceptor.RegisterInterceptor<int>(ConnectorGuid, (_) => new DefaultConnectorValueTransferInterceptionResult<int>() { CancelValueTransfer = true });

            Group.TransferValueToLinks(default, Interceptor);

            foreach (var link in Links)
                link.DidNotReceiveWithAnyArgs().SetData(default, default, default);
        }
    }
}
