using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class ConcatenatingIncomingInputLinkGroupTests
{
    public class Constructor
    {
        [Fact]
        public void InitializesNeedsFurtherProcessingWithTrue()
        {
            var connector = Substitute.For<IRuntimeConnector<object?[]>>();
            var links = Array.Empty<IRuntimeLink<object>>();

            var group = new ConcatenatingIncomingInputLinkGroup<object>(connector, links);

            group.NeedsFurtherProcessing.Should().BeTrue();
        }
    }

    public class PoolValidLinks
    {
        [Fact]
        public void ReturnsValuesOfChangedLinks()
        {
            var connector = CreateConnector<object>();
            var linkValue1 = 42;
            var linkValue2 = 666;
            object? linkValue3 = null;
            var link5Value1 = 43;
            var link5Value2 = 44;
            var link5Value3 = 45;
            var link1 = Substitute.For<IRuntimeLink<object>>();
            var link2 = Substitute.For<IRuntimeLink<object>>();
            var link3 = Substitute.For<IRuntimeLink<object>>();
            var link4 = Substitute.For<IRuntimeLink<object>>();
            var link5 = Substitute.For<IQueueingRuntimeLink<object>>();
            link1.Value.Returns(linkValue1);
            link2.Value.Returns(linkValue2);
            link3.Value.Returns(linkValue3);
            link4.Value.Returns(55);
            link5.GetAndClearQueuedValues().Returns([link5Value1, link5Value2, link5Value3]);
            link1.Changed.Returns(true);
            link2.Changed.Returns(true);
            link3.Changed.Returns(true);
            link4.Changed.Returns(false);
            link5.Changed.Returns(true);
            var links = new[] { link1, link2, link3, link4, link5 };
            var linkValuesOfChangedLinks = new[] { linkValue1, linkValue2, linkValue3, link5Value1, link5Value2, link5Value3 };
            var group = new ConcatenatingIncomingInputLinkGroup<object>(connector, links);

            var result = CallPoolValidLinks(group, links, out _);

            result.Should().BeEquivalentTo(linkValuesOfChangedLinks);
        }

        [Fact]
        public void ReturnsEmptyEnumerationIfNoLinkChanged()
        {
            var connector = CreateConnector<int>();
            var link1 = Substitute.For<IRuntimeLink<int>>();
            link1.Changed.Returns(false);
            var links = new[] { link1 };
            var group = new ConcatenatingIncomingInputLinkGroup<int>(connector, links);

            var result = CallPoolValidLinks(group, links, out _);

            result.Should().BeEmpty();
        }

        [Fact]
        public void SetsCreatedValueToTrueIfResultIsNotEmpty()
        {
            var connector = CreateConnector<int>();
            var linkValue1 = 42;
            var link1 = Substitute.For<IRuntimeLink<int>>();
            link1.Value.Returns(linkValue1);
            link1.Changed.Returns(true);
            var links = new[] { link1 };
            var group = new ConcatenatingIncomingInputLinkGroup<int>(connector, links);

            CallPoolValidLinks(group, links, out var valueCreated);

            valueCreated.Should().BeTrue();
        }

        [Fact]
        public void SetsCreatedValueToTrueIfResultIsEmptyButLastResultWasNotEmpty()
        {
            var connector = CreateConnector<int>();
            var linkValue1 = 42;
            var link1 = Substitute.For<IRuntimeLink<int>>();
            link1.Value.Returns(linkValue1);
            link1.Changed.Returns(true);
            var links = new[] { link1 };
            var group = new ConcatenatingIncomingInputLinkGroup<int>(connector, links);
            CallPoolValidLinks(group, links, out _);
            link1.Changed.Returns(false);

            CallPoolValidLinks(group, links, out var valueCreated);

            valueCreated.Should().BeTrue();
        }

        [Fact]
        public void SetsCreatedValueToTrueIfResultIsEmptyAndItsFirstCalled()
        {
            var connector = CreateConnector<int>();
            var linkValue1 = 42;
            var link1 = Substitute.For<IRuntimeLink<int>>();
            link1.Value.Returns(linkValue1);
            link1.Changed.Returns(false);
            var links = new[] { link1 };
            var group = new ConcatenatingIncomingInputLinkGroup<int>(connector, links);

            CallPoolValidLinks(group, links, out var valueCreated);

            valueCreated.Should().BeTrue();
        }

        [Fact]
        public void SetsCreatedValueToFalseIfResultIsEmptyAndLastResultWasAlsoEmpty()
        {
            var connector = CreateConnector<int>();
            var linkValue1 = 42;
            var link1 = Substitute.For<IRuntimeLink<int>>();
            link1.Value.Returns(linkValue1);
            link1.Changed.Returns(false);
            var links = new[] { link1 };
            var group = new ConcatenatingIncomingInputLinkGroup<int>(connector, links);
            CallPoolValidLinks(group, links, out _);

            CallPoolValidLinks(group, links, out var valueCreated);

            valueCreated.Should().BeFalse();
        }

        [Fact]
        public void ResetsNeedsFurtherProcessing()
        {
            var connector = CreateConnector<int>();
            var link1 = Substitute.For<IRuntimeLink<int>>();
            var links = new[] { link1 };
            var group = new ConcatenatingIncomingInputLinkGroup<int>(connector, links);

            CallPoolValidLinks(group, links, out _);

            group.NeedsFurtherProcessing.Should().BeFalse();
        }

        private static IRuntimeConnector<TValue?[]> CreateConnector<TValue>()
        {
            var connector = Substitute.For<IConnector<TValue?[]>, IPoolable>();
            var runtimeConnector = Substitute.For<IRuntimeConnector<TValue?[]>>();
            runtimeConnector.Connector.Returns(connector);
            return runtimeConnector;
        }

        private static TValue[] CallPoolValidLinks<TValue>(ConcatenatingIncomingInputLinkGroup<TValue> linkGroup, IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
        {
            var methodInfo = typeof(ConcatenatingIncomingInputLinkGroup<TValue>).GetMethod("PoolValidLinks", BindingFlags.NonPublic | BindingFlags.Instance)!;
            object?[] parameters = [validLinks, null];
            var result = (TValue[])methodInfo.Invoke(linkGroup, parameters)!;
            createdValue = (bool)parameters[1]!;
            return result;
        }
    }
}
