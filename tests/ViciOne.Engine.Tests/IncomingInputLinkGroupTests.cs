using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class IncomingInputLinkGroupTests
{
    public class Constructor
    {
        [Fact]
        public void StoresConnector()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);

            group.Connector.Should().BeSameAs(connector);
        }

        [Fact]
        public void StoresLinks()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);

            group.Links.Should().BeSameAs(links);
        }
    }

    public class Pool
    {
        [Fact]
        public void PassesLinksToPoolingValidatorToGetResultValidity()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            IRuntimeLink<object>[] links =
            [
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
            ];
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var validator = Substitute.For<IPoolingValidator>();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            group.Pool(validator, interceptor);

            validator.Received().GetResultValidity(links);
        }

        [Theory]
        [InlineData(Validity.Valid)]
        [InlineData(Validity.Invalid)]
        public void SetsValidityReturnedByPoolingValidatorToConnector(Validity validity)
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var links = Array.Empty<IRuntimeLink<object>>();
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var validator = Substitute.For<IPoolingValidator>();
            validator.GetResultValidity<object>(default!).ReturnsForAnyArgs(validity);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            group.Pool(validator, interceptor);

            connector.Received().SetValidityIfNotEqual(validity);
        }

        [Fact]
        public void PassesLinksToPoolingValidatorToSelectValidLinks()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            IRuntimeLink<object>[] links =
            [
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
            ];
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var validator = Substitute.For<IPoolingValidator>();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            group.Pool(validator, interceptor);

            validator.Received().SelectLinks(links);
        }

        [Fact]
        public void CallsPoolValidLinksWithLinksReturnedByPoolingValidator()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            IRuntimeLink<object>[] links =
            [
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
            ];
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var selectedLinks = links.Take(2).ToArray();
            var validator = Substitute.For<IPoolingValidator>();
            validator.SelectLinks<object>(default!).ReturnsForAnyArgs(selectedLinks);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            group.Pool(validator, interceptor);

            group.PoolValidLinksCalled.Should().BeTrue();
            group.PoolValidLinksArg.Should()
                .NotBeNull().And
                .BeSameAs(selectedLinks);
        }

        [Fact]
        public void CallsInterceptorWithValueReturnedByPoolValidLinksIfValueCreated()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var connectorId = Guid.NewGuid();
            connector.Connector.Id.Returns(connectorId);
            var links = Array.Empty<IRuntimeLink<object>>();
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var poolValidLinksResult = 42;
            group.PoolValidLinksResult = poolValidLinksResult;
            var validator = Substitute.For<IPoolingValidator>();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            object? passedInterceptorArg = default;
            interceptor.RegisterInterceptor<object>(connectorId, (arg) =>
            {
                passedInterceptorArg = arg;
                return new DefaultConnectorValueTransferInterceptionResult<object>() { Value = arg };
            });

            group.Pool(validator, interceptor);

            passedInterceptorArg.Should().Be(poolValidLinksResult);
        }

        [Fact]
        public void CallsInterceptorWithValueFromConnectorIfValueNotCreated()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var connectorId = Guid.NewGuid();
            var connectorValue = 4.2;
            connector.Connector.Id.Returns(connectorId);
            connector.Value.Returns(connectorValue);
            var links = Array.Empty<IRuntimeLink<object>>();
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var validator = Substitute.For<IPoolingValidator>();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            object? passedInterceptorArg = default;
            interceptor.RegisterInterceptor<object>(connectorId, (arg) =>
            {
                passedInterceptorArg = arg;
                return new DefaultConnectorValueTransferInterceptionResult<object>() { Value = arg };
            });

            group.Pool(validator, interceptor);

            passedInterceptorArg.Should().Be(connectorValue);
        }

        [Fact]
        public void UsesValueFromInterceptor()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var connectorId = Guid.NewGuid();
            connector.Connector.Id.Returns(connectorId);
            var links = Array.Empty<IRuntimeLink<object>>();
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var validator = Substitute.For<IPoolingValidator>();
            var interceptorResult = 666;
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            interceptor.RegisterInterceptor<object>(connectorId, (_) => new DefaultConnectorValueTransferInterceptionResult<object>() { Value = interceptorResult });

            group.Pool(validator, interceptor);

            connector.Received().SetValueIfNotEqual(interceptorResult);
        }

        [Fact]
        public void DoesNotSetValueIfInterceptorCancelsTransfer()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            var connectorId = Guid.NewGuid();
            connector.Connector.Id.Returns(connectorId);
            var links = Array.Empty<IRuntimeLink<object>>();
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var validator = Substitute.For<IPoolingValidator>();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            interceptor.RegisterInterceptor<object>(connectorId, (_) => new DefaultConnectorValueTransferInterceptionResult<object>() { CancelValueTransfer = true });

            group.Pool(validator, interceptor);

            connector.DidNotReceiveWithAnyArgs().SetValueIfNotEqual(default);
        }

        [Fact]
        public void ResetsLinkChanged()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();
            IRuntimeLink<object>[] links =
            [
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
                Substitute.For<IRuntimeLink<object>>(),
            ];
            var group = new MockIncomingInputLinkGroup<object, object>(connector, links);
            var validator = Substitute.For<IPoolingValidator>();
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            group.Pool(validator, interceptor);

            links[0].Received().GetAndResetChanged();
            links[1].Received().GetAndResetChanged();
            links[2].Received().GetAndResetChanged();
        }
    }

    private sealed class MockIncomingInputLinkGroup<TInput, TLinkValue>(IRuntimeConnector<TInput> connector, IRuntimeLink<TLinkValue>[] links) :
        IncomingInputLinkGroup<TInput, TLinkValue>(connector, links)
    {
        public bool PoolValidLinksCalled { get; private set; }
        public IEnumerable<IRuntimeLink<TLinkValue>>? PoolValidLinksArg { get; private set; }
        public TInput? PoolValidLinksResult { get; set; }

        protected override TInput? PoolValidLinks(IEnumerable<IRuntimeLink<TLinkValue>> validLinks, out bool createdValue)
        {
            PoolValidLinksCalled = true;
            PoolValidLinksArg = validLinks;

            if (PoolValidLinksResult is not null)
            {
                createdValue = true;
                return PoolValidLinksResult;
            }

            createdValue = false;
            return default;
        }
    }
}
