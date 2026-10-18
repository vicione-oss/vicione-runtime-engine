using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class AggregatingIncomingInputLinkGroupTests
{
    public class Constructor
    {
        [Fact]
        public void ThrowsIfConnectorIsNotPoolable()
        {
            var connector = Substitute.For<IRuntimeConnector<object>>();

            Action action = () => _ = new AggregatingIncomingInputLinkGroup<object>(connector, []);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*invalid*");
        }

        [Fact]
        public void ThrowsIfCrossSourceAggregatingPoolingIsNotValid()
        {
            var connector = Substitute.For<IConnector<object>, IPoolable>();
            ((IPoolable)connector).CrossSourceAggregatingPooling.Returns(Substitute.For<IAggregatingPooling<double>>());
            var runtimeConnector = Substitute.For<IRuntimeConnector<object>>();
            runtimeConnector.Connector.Returns(connector);

            Action action = () => _ = new AggregatingIncomingInputLinkGroup<object>(runtimeConnector, []);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*invalid*cross*");
        }

        [Fact]
        public void ThrowsIfPerSourceAggregatingPoolingIsNotValid()
        {
            var connector = Substitute.For<IConnector<object>, IPoolable>();
            ((IPoolable)connector).CrossSourceAggregatingPooling.Returns(Substitute.For<IAggregatingPooling<object>>());
            ((IPoolable)connector).CrossSourcePoolingStrategy.Returns(PoolingStrategy.Aggregate);
            ((IPoolable)connector).PerSourceAggregatingPooling.Returns(Substitute.For<IAggregatingPooling<double>>());
            ((IPoolable)connector).PerSourcePoolingStrategy.Returns(PoolingStrategy.Aggregate);
            var runtimeConnector = Substitute.For<IRuntimeConnector<object>>();
            runtimeConnector.Connector.Returns(connector);

            Action action = () => _ = new AggregatingIncomingInputLinkGroup<object>(runtimeConnector, []);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*invalid*per*");
        }
    }

    public class PoolValidLinks
    {
        [Fact]
        public void CallsPerSourceAggregatingPoolingForEveryQueueableLinkWithQueuedValues()
        {
            (var connector, _, var perSourceAggregatingPooling) = CreateConnector<object>();
            object?[] linkValues1 = [42, 666];
            object?[] linkValues2 = [43, 667];
            object?[] linkValues3 = [null, 668];
            var link1 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link2 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link3 = Substitute.For<IQueueingRuntimeLink<object>>();
            link1.GetAndClearQueuedValues().Returns(linkValues1);
            link2.GetAndClearQueuedValues().Returns(linkValues2);
            link3.GetAndClearQueuedValues().Returns(linkValues3);
            var links = new[] { link1, link2, link3 };
            var group = new AggregatingIncomingInputLinkGroup<object>(connector, links);

            CallPoolValidLinks(group, links, out _);

            perSourceAggregatingPooling!.Received().Aggregate(Arg.Is<IEnumerable<object>>(x => x.SequenceEqual(linkValues1)));
            perSourceAggregatingPooling!.Received().Aggregate(Arg.Is<IEnumerable<object>>(x => x.SequenceEqual(linkValues2)));
            perSourceAggregatingPooling!.Received().Aggregate(Arg.Is<IEnumerable<object>>(x => x.SequenceEqual(linkValues3)));
        }

        [Fact]
        public void DoesNotCallPerSourceAggregatingPoolingForEveryQueueableLinkIfNoValuesQueued()
        {
            (var connector, _, var perSourceAggregatingPooling) = CreateConnector<object>();
            var link1 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link2 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link3 = Substitute.For<IQueueingRuntimeLink<object>>();
            link1.GetAndClearQueuedValues().Returns([]);
            link2.GetAndClearQueuedValues().Returns([]);
            link3.GetAndClearQueuedValues().Returns([]);
            var links = new[] { link1, link2, link3 };
            var group = new AggregatingIncomingInputLinkGroup<object>(connector, links);

            CallPoolValidLinks(group, links, out _);

            perSourceAggregatingPooling!.DidNotReceiveWithAnyArgs().Aggregate(default!);
        }

        [Fact]
        public void StoresResultOfPerSourceAggregatingPoolingForEveryQueueableLink()
        {
            (var connector, _, var perSourceAggregatingPooling) = CreateConnector<object>();
            object?[] linkValues1 = [1];
            object?[] linkValues2 = [1];
            object?[] linkValues3 = [1];
            var link1 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link2 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link3 = Substitute.For<IQueueingRuntimeLink<object>>();
            link1.GetAndClearQueuedValues().Returns(linkValues1);
            link2.GetAndClearQueuedValues().Returns(linkValues2);
            link3.GetAndClearQueuedValues().Returns(linkValues3);
            var expectedValueLink1 = 42;
            var expectedValueLink2 = 666;
            object? expectedValueLink3 = null;
            perSourceAggregatingPooling!.Aggregate(Arg.Is<IEnumerable<object?>>(linkValues1)).Returns(expectedValueLink1);
            perSourceAggregatingPooling!.Aggregate(Arg.Is<IEnumerable<object?>>(linkValues2)).Returns(expectedValueLink2);
            perSourceAggregatingPooling!.Aggregate(Arg.Is<IEnumerable<object?>>(linkValues3)).Returns(expectedValueLink3);
            var links = new[] { link1, link2, link3 };
            var group = new AggregatingIncomingInputLinkGroup<object>(connector, links);

            CallPoolValidLinks(group, links, out _);

            link1.Received().StoreValue(expectedValueLink1);
            link2.Received().StoreValue(expectedValueLink2);
            link3.Received().StoreValue(expectedValueLink3);
        }

        [Fact]
        public void StoresDequeuedValueOfEveryQueueableLinkIfPerSourcePoolingIsQueue()
        {
            (var connector, _, _) = CreateConnector<object>(withPerSourcePooling: false);
            var expectedValueLink1 = 42;
            var expectedValueLink2 = 666;
            var link1 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link2 = Substitute.For<IQueueingRuntimeLink<object>>();
            link1.TryDequeueValue(out Arg.Any<object?>()).Returns(x =>
            {
                x[0] = expectedValueLink1;
                return true;
            });
            link2.TryDequeueValue(out Arg.Any<object?>()).Returns(x =>
            {
                x[0] = expectedValueLink2;
                return true;
            });
            var links = new[] { link1, link2 };
            var group = new AggregatingIncomingInputLinkGroup<object>(connector, links);

            CallPoolValidLinks(group, links, out _);

            link1.Received().StoreValue(expectedValueLink1);
            link2.Received().StoreValue(expectedValueLink2);
        }

        [Fact]
        public void DoesNotCallStoreValueIfPerSourcePoolingIsQueueButDequeueWasNotSuccessful()
        {
            (var connector, _, _) = CreateConnector<object>(withPerSourcePooling: false);
            var link1 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link2 = Substitute.For<IQueueingRuntimeLink<object>>();
            link1.TryDequeueValue(out Arg.Any<object?>()).Returns(false);
            link2.TryDequeueValue(out Arg.Any<object?>()).Returns(false);
            var links = new[] { link1, link2 };
            var group = new AggregatingIncomingInputLinkGroup<object>(connector, links);

            CallPoolValidLinks(group, links, out _);

            link1.DidNotReceiveWithAnyArgs().StoreValue(Arg.Any<object?>());
            link2.DidNotReceiveWithAnyArgs().StoreValue(Arg.Any<object?>());
        }

        [Fact]
        public void CallsCrossSourceAggregatingPoolingOnlyWithValuesOfCurrentCall()
        {
            (var connector, var crossSourceAggregatingPooling, _) = CreateConnector<object>();
            var aggregatedValues = new List<object?[]>();
            crossSourceAggregatingPooling.Aggregate(Arg.Do<IEnumerable<object?>>(x => aggregatedValues.Add([.. x])));
            var link1 = Substitute.For<IRuntimeLink<object>>();
            var link2 = Substitute.For<IRuntimeLink<object>>();
            link1.Value.Returns(42);
            link2.Value.Returns(666);
            var links = new[] { link1, link2 };
            var group = new AggregatingIncomingInputLinkGroup<object>(connector, links);

            CallPoolValidLinks(group, links, out _);
            CallPoolValidLinks(group, [link2], out _);

            aggregatedValues.Should().HaveCount(2);
            aggregatedValues[1].Should().Equal(666);
        }

        [Fact]
        public void CallsCrossSourceAggregatingPoolingWithValuesFromValidLinks()
        {
            (var connector, var crossSourceAggregatingPooling, _) = CreateConnector<object>();
            var linkValue1 = 42;
            var linkValue2 = 666;
            object? linkValue3 = null;
            var link1 = Substitute.For<IRuntimeLink<object>>();
            var link2 = Substitute.For<IRuntimeLink<object>>();
            var link3 = Substitute.For<IRuntimeLink<object>>();
            link1.Value.Returns(linkValue1);
            link2.Value.Returns(linkValue2);
            link3.Value.Returns(linkValue3);
            var links = new[] { link1, link2, link3 };
            var linkValues = new[] { linkValue1, linkValue2, linkValue3 };
            var group = new AggregatingIncomingInputLinkGroup<object>(connector, links);

            CallPoolValidLinks(group, links, out _);

            crossSourceAggregatingPooling.Received().Aggregate(Arg.Is<IEnumerable<object>>(x => x.SequenceEqual(linkValues)));
        }

        [Fact]
        public void DoesNotCallCrossSourceAggregatingPoolingIfNoValuesAvailable()
        {
            (var connector, var crossSourceAggregatingPooling, _) = CreateConnector<int>();
            var group = new AggregatingIncomingInputLinkGroup<int>(connector, []);

            CallPoolValidLinks(group, [], out _);

            crossSourceAggregatingPooling.DidNotReceiveWithAnyArgs().Aggregate(Arg.Any<IEnumerable<int>>());
        }

        [Fact]
        public void ReturnsResultFromCrossSourceAggregatingPooling()
        {
            (var connector, var crossSourceAggregatingPooling, _) = CreateConnector<int>();
            var expected = 42;
            crossSourceAggregatingPooling.Aggregate(Arg.Any<IEnumerable<int>>()).Returns(expected);
            var link1 = Substitute.For<IRuntimeLink<int>>();
            var link2 = Substitute.For<IRuntimeLink<int>>();
            var links = new[] { link1, link2 };
            var group = new AggregatingIncomingInputLinkGroup<int>(connector, links);

            var result = CallPoolValidLinks(group, links, out _);

            result.Should().Be(expected);
        }

        [Fact]
        public void SetsCreatedValueToTrueIfAggregatingPoolingWasCalled()
        {
            (var connector, _, _) = CreateConnector<int>();
            var link1 = Substitute.For<IRuntimeLink<int>>();
            var link2 = Substitute.For<IRuntimeLink<int>>();
            var links = new[] { link1, link2 };
            var group = new AggregatingIncomingInputLinkGroup<int>(connector, links);

            CallPoolValidLinks(group, links, out var createdValue);

            createdValue.Should().BeTrue();
        }

        [Fact]
        public void SetsCreatedValueToFalseIfAggregatingPoolingWasNotCalled()
        {
            (var connector, _, _) = CreateConnector<int>();
            var group = new AggregatingIncomingInputLinkGroup<int>(connector, []);

            CallPoolValidLinks(group, [], out var createdValue);

            createdValue.Should().BeFalse();
        }

        private static (IRuntimeConnector<TValue>, IAggregatingPooling<TValue>, IAggregatingPooling<TValue>?) CreateConnector<TValue>(
            bool withPerSourcePooling = true)
        {
            var connector = Substitute.For<IConnector<TValue>, IPoolable>();
            var crossSourceAggregatingPooling = Substitute.For<IAggregatingPooling<TValue>>();
            var perSourceAggregatingPooling = withPerSourcePooling ? Substitute.For<IAggregatingPooling<TValue>>() : null;
            var perSourcePoolingStrategy = withPerSourcePooling ? PoolingStrategy.Aggregate : PoolingStrategy.Queue;
            ((IPoolable)connector).CrossSourceAggregatingPooling.Returns(crossSourceAggregatingPooling);
            ((IPoolable)connector).CrossSourcePoolingStrategy.Returns(PoolingStrategy.Aggregate);
            ((IPoolable)connector).PerSourceAggregatingPooling.Returns(perSourceAggregatingPooling);
            ((IPoolable)connector).PerSourcePoolingStrategy.Returns(perSourcePoolingStrategy);
            var runtimeConnector = Substitute.For<IRuntimeConnector<TValue>>();
            runtimeConnector.Connector.Returns(connector);
            return (runtimeConnector, crossSourceAggregatingPooling, perSourceAggregatingPooling);
        }

        private static TValue? CallPoolValidLinks<TValue>(AggregatingIncomingInputLinkGroup<TValue> linkGroup, IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
        {
            var methodInfo = typeof(AggregatingIncomingInputLinkGroup<TValue>).GetMethod("PoolValidLinks", BindingFlags.NonPublic | BindingFlags.Instance)!;
            object?[] parameters = [validLinks, null];
            var result = (TValue?)methodInfo.Invoke(linkGroup, parameters);
            createdValue = (bool)parameters[1]!;
            return result;
        }
    }
}
