using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Engine.Extensions;
using Xunit;

public class LinkExtensionsTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Bezeichner dürfen kein falsches Suffix aufweisen", Justification = "Die Klasse stellt den zu testenden Methodennamen dar.")]
    public class DetermineIfRuntimeLinkNeedsQueue
    {
        [Fact]
        public void ReturnsFalseIfDestinationIsNotPoolable()
        {
            var destination = Substitute.For<IConnector>();
            var link = Substitute.For<ILinkWithDestination>();
            link.Destination.Returns(destination);

            var result = LinkExtensions.DetermineIfRuntimeLinkNeedsQueue(link);

            result.Should().BeFalse();
        }

        [Fact]
        public void ReturnsTrueIfDestinationCrossSourcePoolingStrategyIsNotQueue()
        {
            var link = CreateLink(PoolingStrategy.Aggregate, PoolingStrategy.Queue);

            var result = LinkExtensions.DetermineIfRuntimeLinkNeedsQueue(link);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsTrueIfDestinationPerSourcePoolingStrategyIsNotQueue()
        {
            var link = CreateLink(PoolingStrategy.Queue, PoolingStrategy.Aggregate);

            var result = LinkExtensions.DetermineIfRuntimeLinkNeedsQueue(link);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfBothPoolingStrategiesOfDestinationAreQueue()
        {
            var link = CreateLink(PoolingStrategy.Queue, PoolingStrategy.Queue);

            var result = LinkExtensions.DetermineIfRuntimeLinkNeedsQueue(link);

            result.Should().BeFalse();
        }

        private static ILinkWithDestination CreateLink(PoolingStrategy crossSourceStrategy, PoolingStrategy perSourceStrategy)
        {
            var destination = Substitute.For<IPoolable, IConnector>();
            destination.CrossSourcePoolingStrategy.Returns(crossSourceStrategy);
            destination.PerSourcePoolingStrategy.Returns(perSourceStrategy);
            var link = Substitute.For<ILinkWithDestination>();
            link.Destination.Returns((IConnector)destination);
            return link;
        }
    }
}
