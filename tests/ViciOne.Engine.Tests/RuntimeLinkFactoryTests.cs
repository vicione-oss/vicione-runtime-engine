using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeLinkFactoryTests
{
    public class Create
    {
        [Fact]
        public void CreatesInstanceWithCorrectLink()
        {
            var link = CreateInternalLink<int, int, int>();
            var connectedConnectors = MockConnectedConnectors<int>();
            using var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            var runtimeLink = factory.Create(link, connectedConnectors);

            runtimeLink.Link.Should().BeSameAs(link);
        }

        public static TheoryData<ILink, IDictionary<Guid, IRuntimeConnector>, Type> ArgumentsFor_CreatesCorrectRuntimeLinkTypeAccordingLinkType
        {
            get
            {
                var connectedConnectors = MockConnectedConnectors<int>();
                return new()
                {
                    { CreateInternalLink<int, int, int>(), connectedConnectors, typeof(InternalRuntimeLink<int, int, int>) },
                    { CreateInternalLink<int, string, string>(), connectedConnectors, typeof(InternalRuntimeLink<int, string, string>) },
                    { CreateInternalLink<int, double, double[]>(), connectedConnectors, typeof(InternalRuntimeLink<int, double, double[]>) },
                    { CreateExternalIncomingLink<int, int, int>(), connectedConnectors, typeof(ExternalIncomingRuntimeLink<int, int, int>) },
                    { CreateExternalIncomingLink<int, int, string>(), connectedConnectors, typeof(ExternalIncomingRuntimeLink<int, int, string>) },
                    { CreateExternalIncomingLink<short, int, string>(), connectedConnectors, typeof(ExternalIncomingRuntimeLink<short, int, string>) },
                    { CreateExternalIncomingLink<double, double, double[]>(), connectedConnectors, typeof(ExternalIncomingRuntimeLink<double, double, double[]>) },
                    { CreateExternalOutgoingLink<int>(), connectedConnectors, typeof(ExternalOutgoingRuntimeLink<int>) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_CreatesCorrectRuntimeLinkTypeAccordingLinkType))]
        public void CreatesCorrectRuntimeLinkTypeAccordingLinkType(ILink link, IDictionary<Guid, IRuntimeConnector> connectedConnectors, Type expectedType)
        {
            using var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            var runtimeLink = factory.Create(link, connectedConnectors);

            runtimeLink.Should().BeOfType(expectedType);
        }

        public static TheoryData<ILink, IDictionary<Guid, IRuntimeConnector>, Type> ArgumentsFor_CreatesQueueingRuntimeLinkIfNeccessary
        {
            get
            {
                var connectedConnectors = MockConnectedConnectors<int>();
                return new()
                {
                    { CreateInternalLink<int, int, int>(crossSourcePoolingStrategy: PoolingStrategy.Concat), connectedConnectors, typeof(InternalQueueingRuntimeLink<int, int, int>) },
                    { CreateInternalLink<int, int, int>(perSourcePoolingStrategy: PoolingStrategy.Aggregate), connectedConnectors, typeof(InternalQueueingRuntimeLink<int, int, int>) },
                    { CreateExternalIncomingLink<int, int, int>(crossSourcePoolingStrategy: PoolingStrategy.Concat), connectedConnectors, typeof(ExternalIncomingQueueingRuntimeLink<int, int, int>) },
                    { CreateExternalIncomingLink<int, int, int>(perSourcePoolingStrategy: PoolingStrategy.Aggregate), connectedConnectors, typeof(ExternalIncomingQueueingRuntimeLink<int, int, int>) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_CreatesQueueingRuntimeLinkIfNeccessary))]
        public void CreatesQueueingRuntimeLinkIfNeccessary(ILink link, IDictionary<Guid, IRuntimeConnector> connectedConnectors, Type expectedType)
        {
            using var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            var runtimeLink = factory.Create(link, connectedConnectors);

            runtimeLink.Should().BeOfType(expectedType);
        }

        [Fact]
        public void DoesNotCreateQueueingRuntimeLinkIfDestinationIsOnOtherEngine()
        {
            var link = CreateInternalLink<int, int, int>(PoolingStrategy.Concat);
            var connectedConnectors = new Dictionary<Guid, IRuntimeConnector>()
            {
                { link.Source.Id, CreateRuntimeConnector(link.Source, null) },
            };
            using var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            var runtimeLink = factory.Create(link, connectedConnectors);

            runtimeLink.Should().NotBeOfType<InternalQueueingRuntimeLink<int, int, int>>();
        }

        public static TheoryData<ILink, IDictionary<Guid, IRuntimeConnector>, object> ArgumentsFor_PassesCorrectDefaultValue => new()
        {
            { CreateInternalLink<int, int, int>(), MockConnectedConnectors<int>(42), 42 },
            { CreateExternalIncomingLink<int, int, int>(4), MockConnectedConnectors<int>(), 4 },
            { CreateExternalOutgoingLink<int>(), MockConnectedConnectors<int>(666), 666 },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_PassesCorrectDefaultValue))]
        public void PassesCorrectDefaultValue(ILink link, IDictionary<Guid, IRuntimeConnector> connectedConnectors, object expectedDefaultValue)
        {
            var mockTypeConverter = (IMockTypeConverter)link.TypeConversion!.TypeConverter;
            using var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            factory.Create(link, connectedConnectors);

            mockTypeConverter.ReceivedValue.Should().Be(expectedDefaultValue);
        }

        [Fact]
        public void ThrowsOnUnknownLinkType()
        {
            var link = new UnknownLink();
            using var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            Action action = () => factory.Create(link, Substitute.For<IDictionary<Guid, IRuntimeConnector>>());

            action.Should()
                .Throw<NotImplementedException>()
                .WithMessage($"*{link.GetType()}*not*implemented*");
        }

        private static IInternalLink CreateInternalLink<TSource, TValue, TDestination>(
            PoolingStrategy crossSourcePoolingStrategy = PoolingStrategy.Queue,
            PoolingStrategy perSourcePoolingStrategy = PoolingStrategy.Queue)
        {
            var link = Substitute.For<IInternalLink, IInternalLink<TSource, TValue, TDestination>>();
            var sourceConnector = Substitute.For<IConnector<TSource>>();
            sourceConnector.Id.Returns(Guid.NewGuid());
            link.Source.Returns(sourceConnector);
            var destinationConnector = Substitute.For<IConnector<TDestination>, IPoolable>();
            ((IPoolable)destinationConnector).CrossSourcePoolingStrategy.Returns(crossSourcePoolingStrategy);
            ((IPoolable)destinationConnector).PerSourcePoolingStrategy.Returns(perSourcePoolingStrategy);
            destinationConnector.Id.Returns(Guid.NewGuid());
            link.Destination.Returns(destinationConnector);
            ((IInternalLink<TSource, TValue, TDestination>)link).Destination.Returns(destinationConnector);
            link.TypeConversion!.TypeConverter.Returns(new MockTypeConverter<TValue>());
            return link;
        }

        private static IExternalIncomingLink CreateExternalIncomingLink<TSource, TValue, TDestination>(TValue? defaultValue = default,
            PoolingStrategy crossSourcePoolingStrategy = PoolingStrategy.Queue,
            PoolingStrategy perSourcePoolingStrategy = PoolingStrategy.Queue)
        {
            var link = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<TSource, TValue, TDestination>>();
            var destination = Substitute.For<IConnector<TDestination>, IPoolable>();
            ((IPoolable)destination).CrossSourcePoolingStrategy.Returns(crossSourcePoolingStrategy);
            ((IPoolable)destination).PerSourcePoolingStrategy.Returns(perSourcePoolingStrategy);
            link.Destination.Returns(destination);
            ((IExternalIncomingLink<TSource, TValue, TDestination>)link).Destination.Returns(destination);
            link.TypeConversion!.TypeConverter.Returns(new MockTypeConverter<TValue>());
            ((IExternalIncomingLink<TSource, TValue, TDestination>)link).DefaultValue.Returns(defaultValue);
            return link;
        }

        private static IExternalOutgoingLink CreateExternalOutgoingLink<TSource>()
        {
            var link = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<TSource>>();
            link.Source.Returns(Substitute.For<IConnector<TSource>>());
            link.TypeConversion!.TypeConverter.Returns(new MockTypeConverter<TSource>());
            return link;
        }

        private static IDictionary<Guid, IRuntimeConnector> MockConnectedConnectors<T>(object? defaultValue = null)
        {
            var connectedConnectors = Substitute.For<IDictionary<Guid, IRuntimeConnector>>();
            connectedConnectors[default!].ReturnsForAnyArgs(callInfo =>
            {
                return CreateRuntimeConnector<T>(defaultValue);
            });
            connectedConnectors.TryGetValue(default!, out Arg.Any<IRuntimeConnector>()!).ReturnsForAnyArgs(callInfo =>
            {
                callInfo[1] = CreateRuntimeConnector<T>(defaultValue);
                return true;
            });
            connectedConnectors.ContainsKey(default!).ReturnsForAnyArgs(true);

            return connectedConnectors;
        }

        private static IRuntimeConnector CreateRuntimeConnector<T>(object? defaultValue)
            => CreateRuntimeConnector(Substitute.For<IConnector<T>>(), defaultValue);

        private static IRuntimeConnector CreateRuntimeConnector(object connector, object? defaultValue)
        {
            var valueType = DataTypeCompatibilityValidator.DetermineValueType(connector);
            var runtimeConnector = (IRuntimeConnector)Substitute.For([typeof(IRuntimeConnector<>).MakeGenericType(valueType)], []);
            if (defaultValue is null)
            {
                if (valueType == typeof(string))
                    defaultValue = string.Empty;
                else if (valueType.IsArray)
                    defaultValue = Array.CreateInstance(valueType.GetElementType()!, 0);
            }
            runtimeConnector.Value.Returns(defaultValue ?? Activator.CreateInstance(valueType));
            return runtimeConnector;
        }

        private interface IUnknownLink : ILink { }
        private sealed class UnknownLink : IUnknownLink
        {
            public ITypeConversion? TypeConversion => throw new NotImplementedException();
            public object? DefaultValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Guid Id => throw new NotImplementedException();
        }

        private interface IMockTypeConverter
        {
            object? ReceivedValue { get; }
        }

        private sealed class MockTypeConverter<TValue> : TypeConverter, IMockTypeConverter
        {
            public object? ReceivedValue { get; private set; }

            public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
            {
                ReceivedValue = value;
                return default(TValue);
            }
        }
    }

    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            Action action = factory.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var factory = new RuntimeLinkFactory(EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            factory.Dispose();

            Action action = factory.Dispose;

            action.Should().NotThrow();
        }
    }
}
