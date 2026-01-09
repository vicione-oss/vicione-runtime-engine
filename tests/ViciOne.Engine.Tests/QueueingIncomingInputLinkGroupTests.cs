using AwesomeAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class QueueingIncomingInputLinkGroupTests
{
    // TODO: ist die Benennung richtig?
    public class LinkChanged
    {
        [Fact]
        public void EnqueuesValueIfLinkIsValid()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), [link], () => validator);
            var linkValue = 42;
            link.Value.Returns(linkValue);

            link.LinkChanged += Raise.Event();

            group.NeedsFurtherProcessing.Should().BeTrue();
        }

        [Fact]
        public void DoesNotEqueueValueIfLinkIsNotValid()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(false);
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), [link], () => validator);
            var linkValue = 42;
            link.Value.Returns(linkValue);

            link.LinkChanged += Raise.Event();

            group.NeedsFurtherProcessing.Should().BeFalse();
        }

        [Fact]
        public void ResetsLinkChanged()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(false);
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), [link], () => validator);

            link.LinkChanged += Raise.Event();

            link.Received().GetAndResetChanged();
        }
    }

    public class NeedsFurtherProcessing
    {
        [Fact]
        public void ReturnsTrueIfQueueIsNotEmpty()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), [link], () => validator);
            link.LinkChanged += Raise.Event();

            var result = group.NeedsFurtherProcessing;

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfQueueIsEmpty()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var validator = Substitute.For<IPoolingValidator>();
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), [link], () => validator);

            var result = group.NeedsFurtherProcessing;

            result.Should().BeFalse();
        }

        [Fact]
        public void ReturnsTrueIfQueueIsEmptyButPerSourceAggregatingPoolingIsNotNullAndAnyLinkIsChanged()
        {
            var connector = Substitute.For<IConnector<int>, IPoolable>();
            ((IPoolable)connector).PerSourceAggregatingPooling.Returns(Substitute.For<IAggregatingPooling<int>>());
            ((IPoolable)connector).PerSourcePoolingStrategy.Returns(PoolingStrategy.Aggregate);
            var runtimeConnector = Substitute.For<IRuntimeConnector<int>>();
            runtimeConnector.Connector.Returns(connector);
            var link = Substitute.For<IRuntimeLink<int>>();
            link.Changed.Returns(true);
            var validator = Substitute.For<IPoolingValidator>();
            var group = new QueueingIncomingInputLinkGroup<int>(runtimeConnector, [link], () => validator);

            var result = group.NeedsFurtherProcessing;

            result.Should().BeTrue();
        }
    }

    public class PoolValidLinks
    {
        [Fact]
        public void ReturnsNextValueInQueue()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), [link], () => validator);
            var linkValue1 = 42;
            link.Value.Returns(linkValue1);
            link.LinkChanged += Raise.Event();
            var linkValue2 = 666;
            link.Value.Returns(linkValue2);
            link.LinkChanged += Raise.Event();

            var result1 = CallPoolValidLinks(group, [], out _);
            var result2 = CallPoolValidLinks(group, [], out _);

            result1.Should().Be(linkValue1);
            result2.Should().Be(linkValue2);
        }

        [Fact]
        public void SetsCreatedValueToTrueIfQueueWasNotEmpty()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), [link], () => validator);
            link.LinkChanged += Raise.Event();

            CallPoolValidLinks(group, [], out var valueCreated);

            valueCreated.Should().BeTrue();
        }

        [Fact]
        public void SetsCreatedValueToFalseIfQueueWasEmpty()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            IRuntimeLink<int>[] links = [link];
            var validator = Substitute.For<IPoolingValidator>();
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), links, () => validator);

            CallPoolValidLinks(group, links, out var valueCreated);

            valueCreated.Should().BeFalse();
        }

        [Fact]
        public void LimitsQueuedEntriesToMaximumCapacity()
        {
            var link1 = Substitute.For<IRuntimeLink<int>>();
            var link2 = Substitute.For<IRuntimeLink<int>>();
            IRuntimeLink<int>[] links = [link1, link2];
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<int>(Substitute.For<IRuntimeConnector<int>>(), links, () => validator);
            var expectedCount = QueueingIncomingInputLinkGroup<int>.MaximumCapacityPerLink * links.Length;
            var overflowCount = 10;
            var valuesToQueue = Enumerable.Range(1, expectedCount + overflowCount).ToArray();
            var expectedValues = valuesToQueue.Skip(overflowCount);
            foreach (var value in valuesToQueue)
            {
                link1.Value.Returns(value);
                link1.LinkChanged += Raise.Event();
            }

            var returnedValues = new List<int>();
            for (var i = 0; i < valuesToQueue.Length; i++)
            {
                var returnedValue = CallPoolValidLinks(group, [], out var valueCreated);
                if (valueCreated)
                    returnedValues.Add(returnedValue);
            }

            returnedValues.Should().BeEquivalentTo(expectedValues);
        }

        [Fact]
        public void CallsPerSourceAggregatingPoolingForEveryQueueableLinkWithQueuedValues()
        {
            (var connector, var aggregatingPooling) = CreateConnectorWithPerSourceAggregatingPooling<object>();
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
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<object>(connector, links, () => validator);

            CallPoolValidLinks(group, links, out _);

            aggregatingPooling.Received().Aggregate(Arg.Is<IEnumerable<object>>(x => x.SequenceEqual(linkValues1)));
            aggregatingPooling.Received().Aggregate(Arg.Is<IEnumerable<object>>(x => x.SequenceEqual(linkValues2)));
            aggregatingPooling.Received().Aggregate(Arg.Is<IEnumerable<object>>(x => x.SequenceEqual(linkValues3)));
        }

        [Fact]
        public void DoesNotCallPerSourceAggregatingPoolingForEveryQueueableLinkIfNoValuesQueued()
        {
            (var connector, var aggregatingPooling) = CreateConnectorWithPerSourceAggregatingPooling<object>();
            var link1 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link2 = Substitute.For<IQueueingRuntimeLink<object>>();
            var link3 = Substitute.For<IQueueingRuntimeLink<object>>();
            link1.GetAndClearQueuedValues().Returns([]);
            link2.GetAndClearQueuedValues().Returns([]);
            link3.GetAndClearQueuedValues().Returns([]);
            var links = new[] { link1, link2, link3 };
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<object>(connector, links, () => validator);

            CallPoolValidLinks(group, links, out _);

            aggregatingPooling.DidNotReceiveWithAnyArgs().Aggregate(default!);
        }

        [Fact]
        public void StoresResultOfPerSourceAggregatingPoolingForEveryQueueableLink()
        {
            (var connector, var aggregatingPooling) = CreateConnectorWithPerSourceAggregatingPooling<object>();
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
            aggregatingPooling.Aggregate(Arg.Is<IEnumerable<object?>>(linkValues1)).Returns(expectedValueLink1);
            aggregatingPooling.Aggregate(Arg.Is<IEnumerable<object?>>(linkValues2)).Returns(expectedValueLink2);
            aggregatingPooling.Aggregate(Arg.Is<IEnumerable<object?>>(linkValues3)).Returns(expectedValueLink3);
            var links = new[] { link1, link2, link3 };
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<object>(connector, links, () => validator);

            CallPoolValidLinks(group, links, out _);

            link1.Received().StoreValue(expectedValueLink1);
            link2.Received().StoreValue(expectedValueLink2);
            link3.Received().StoreValue(expectedValueLink3);
        }

        [Fact]
        public void EnqueuesResultOfPerSourceAggregatingPoolingForEveryQueueableLink()
        {
            (var connector, var aggregatingPooling) = CreateConnectorWithPerSourceAggregatingPooling<int>();
            int[] linkValues = [1];
            var expectedPoolingResult = 42;
            var link = Substitute.For<IQueueingRuntimeLink<int>>();
            link.GetAndClearQueuedValues().Returns(linkValues);
            link.Value.Returns(expectedPoolingResult);
            var links = new[] { link };
            var validator = Substitute.For<IPoolingValidator>();
            validator.IsLinkValid(Arg.Any<IRuntimeLink<int>>()).ReturnsForAnyArgs(true);
            var group = new QueueingIncomingInputLinkGroup<int>(connector, links, () => validator);

            var result = CallPoolValidLinks(group, links, out _);

            result.Should().Be(expectedPoolingResult);
        }

        private static (IRuntimeConnector<TValue>, IAggregatingPooling<TValue>) CreateConnectorWithPerSourceAggregatingPooling<TValue>()
        {
            var connector = Substitute.For<IConnector<TValue>, IPoolable>();
            var aggregatingPooling = Substitute.For<IAggregatingPooling<TValue>>();
            ((IPoolable)connector).CrossSourcePoolingStrategy.Returns(PoolingStrategy.Queue);
            ((IPoolable)connector).PerSourceAggregatingPooling.Returns(aggregatingPooling);
            ((IPoolable)connector).PerSourcePoolingStrategy.Returns(PoolingStrategy.Aggregate);
            var runtimeConnector = Substitute.For<IRuntimeConnector<TValue>>();
            runtimeConnector.Connector.Returns(connector);
            return (runtimeConnector, aggregatingPooling);
        }

        private static TValue CallPoolValidLinks<TValue>(QueueingIncomingInputLinkGroup<TValue> linkGroup, IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
        {
            var methodInfo = typeof(QueueingIncomingInputLinkGroup<TValue>).GetMethod("PoolValidLinks", BindingFlags.NonPublic | BindingFlags.Instance)!;
            object?[] parameters = [validLinks, null];
            var result = (TValue)methodInfo.Invoke(linkGroup, parameters)!;
            createdValue = (bool)parameters[1]!;
            return result;
        }
    }
}
