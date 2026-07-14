using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class IncomingInputLinkGroupFactoryTests
{
    public class Create
    {
        [Fact]
        public void CreatesInstanceWithCorrectConnectorAndLinksAndGetValidatorDelegate()
        {
            var connector = CreateConnector<int>();
            IRuntimeLink[] links =
            [
                Substitute.For<IRuntimeLink<int>>(),
                Substitute.For<IRuntimeLink<int>>(),
            ];
            using var factory = new IncomingInputLinkGroupFactory(EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink);

            var entry = factory.Create(connector, links, () => Substitute.For<IPoolingValidator>());

            entry.Should().NotBeNull();
            entry.Connector.Should().BeSameAs(connector);
            entry.Links.Should().BeEquivalentTo(links);
        }

        public static TheoryData<IRuntimeConnector, Type> ArgumentsFor_CreatesCorrectLinkGroupTypeAccordingToPoolingStrategy => new()
        {
            { CreateConnector<int>(PoolingStrategy.Queue), typeof(QueueingIncomingInputLinkGroup<int>) },
            { CreateConnector<int>(PoolingStrategy.Aggregate), typeof(AggregatingIncomingInputLinkGroup<int>) },
            { CreateConnector<int[]>(PoolingStrategy.Concat), typeof(ConcatenatingIncomingInputLinkGroup<int>) },
            { CreateConnector<object?[]>(PoolingStrategy.Concat), typeof(ConcatenatingIncomingInputLinkGroup<object>) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_CreatesCorrectLinkGroupTypeAccordingToPoolingStrategy))]
        public void CreatesCorrectLinkGroupTypeAccordingToPoolingStrategy(IRuntimeConnector connector, Type expectedType)
        {
            using var factory = new IncomingInputLinkGroupFactory(EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink);

            var entry = factory.Create(connector, [], () => Substitute.For<IPoolingValidator>());

            entry.Should().BeOfType(expectedType);
        }

        [Fact]
        public void ThrowsOnUnknownPoolingStrategy()
        {
            var connector = CreateConnector<int>((PoolingStrategy)int.MaxValue);
            using var factory = new IncomingInputLinkGroupFactory(EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink);

            Action action = () => factory.Create(connector, [], () => Substitute.For<IPoolingValidator>());

            action.Should()
                .Throw<NotImplementedException>()
                .WithMessage($"*{nameof(PoolingStrategy)}*not*implemented*");
        }

        private static IRuntimeConnector<TValue> CreateConnector<TValue>(PoolingStrategy poolingStrategy = PoolingStrategy.Queue)
        {
            var connector = Substitute.For<IConnector<TValue>, IPoolable>();
            ((IPoolable)connector).CrossSourcePoolingStrategy.Returns(poolingStrategy);
            if (poolingStrategy == PoolingStrategy.Aggregate)
                ((IPoolable)connector).CrossSourceAggregatingPooling.Returns(Substitute.For<IAggregatingPooling<TValue>>());
            var runtimeConnector = Substitute.For<IRuntimeConnector<TValue>>();
            runtimeConnector.Connector.Returns(connector);
            ((IRuntimeConnector)runtimeConnector).Connector.Returns(connector);
            return runtimeConnector;
        }
    }
}
