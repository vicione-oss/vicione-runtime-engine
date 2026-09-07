using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using Xunit;

public class DataTypeCompatibilityValidatorTests
{
    public class CanConvert
    {
        [Fact]
        public void ReturnsTrueIfShouldConvertFromAndCanConvertFrom()
        {
            var conversion = Substitute.For<ITypeConversion>();
            conversion.UseConvertFrom.Returns(true);
            conversion.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));

            var result = DataTypeCompatibilityValidator.CanConvert(conversion, typeof(string), typeof(int));

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsTrueIfShouldConvertToAndCanConvertTo()
        {
            var conversion = Substitute.For<ITypeConversion>();
            conversion.UseConvertFrom.Returns(false);
            conversion.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));

            var result = DataTypeCompatibilityValidator.CanConvert(conversion, typeof(int), typeof(byte));

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfShouldConvertFromButCannotConvertFrom()
        {
            var conversion = Substitute.For<ITypeConversion>();
            conversion.UseConvertFrom.Returns(true);
            conversion.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));

            var result = DataTypeCompatibilityValidator.CanConvert(conversion, typeof(double), typeof(int));

            result.Should().BeFalse();
        }

        [Fact]
        public void ReturnsFalseIfShouldConvertToButCannotConvertTo()
        {
            var conversion = Substitute.For<ITypeConversion>();
            conversion.UseConvertFrom.Returns(false);
            conversion.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));

            var result = DataTypeCompatibilityValidator.CanConvert(conversion, typeof(int), typeof(decimal));

            result.Should().BeFalse();
        }
    }

    public class DetermineExternalIncomingLinkSourceType
    {
        public static TheoryData<IExternalIncomingLink, Type> ArgumentsFor_CanDetermineExternalIncomingLinkSourceType => new()
        {
            { Substitute.For<IExternalIncomingLink<object, object, object>>(), typeof(object) },
            { Substitute.For<IExternalIncomingLink<bool, object, object>>(), typeof(bool) },
            { Substitute.For<IExternalIncomingLink<int, object, object>>(), typeof(int) },
            { Substitute.For<IExternalIncomingLink<short, object, object>>(), typeof(short) },
            { Substitute.For<IExternalIncomingLink<long, object, object>>(), typeof(long) },
            { Substitute.For<IExternalIncomingLink<double, object, object>>(), typeof(double) },
            { Substitute.For<IExternalIncomingLink<string, object, object>>(), typeof(string) },
            { Substitute.For<IExternalIncomingLinkWithAdditionalType<object, float, object, object>>(), typeof(float) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_CanDetermineExternalIncomingLinkSourceType))]
        public void CanDetermineExternalIncomingLinkSourceType(IExternalIncomingLink link, Type expected)
        {
            var determined = DataTypeCompatibilityValidator.DetermineExternalIncomingLinkSourceType(link);

            determined.Should().BeSameAs(expected);
        }

        [Fact]
        public void ThrowsIfSourceTypeCannotBeDetermined()
        {
            var link = Substitute.For<IExternalIncomingLink>();

            Action action = () => DataTypeCompatibilityValidator.DetermineExternalIncomingLinkSourceType(link);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*source*type*not*determine*");
        }

        public interface IExternalIncomingLinkWithAdditionalType<TAdditional, TSource, TValue, TDestination> : IExternalIncomingLink<TSource, TValue, TDestination> { }
    }

    public class DetermineLinkValueType
    {
        public static TheoryData<IRuntimeConnector, Type> ArgumentsFor_CanDetermineLinkValueType
        {
            get
            {
                return new()
                {
                    { CreateInput<object>(PoolingStrategy.Aggregate), typeof(object) },
                    { CreateInput<object>(PoolingStrategy.Queue), typeof(object)  },
                    { CreateInput<object[]>(PoolingStrategy.Concat), typeof(object) },
                    { CreateInput<object[][]>(PoolingStrategy.Concat), typeof(object[]) },
                    { CreateInput<object[]>(PoolingStrategy.Aggregate), typeof(object[]) },
                    { CreateInput<object[][]>(PoolingStrategy.Aggregate), typeof(object[][]) },
                    { CreateInput<double>(PoolingStrategy.Aggregate), typeof(double) },
                    { CreateInput<double>(PoolingStrategy.Queue), typeof(double) },
                    { CreateInput<double[]>(PoolingStrategy.Concat), typeof(double) },
                    { CreateInput<double[][]>(PoolingStrategy.Concat), typeof(double[]) },
                    { CreateInput<double[]>(PoolingStrategy.Aggregate), typeof(double[]) },
                    { CreateInput<double[][]>(PoolingStrategy.Aggregate), typeof(double[][]) },
                    { CreateOutput<object>(), typeof(object) },
                    { CreateOutput<object[]>(), typeof(object[]) },
                    { CreateOutput<object[][]>(), typeof(object[][]) },
                    { CreateOutput<double>(), typeof(double) },
                    { CreateOutput<double[]>(), typeof(double[]) },
                    { CreateOutput<double[][]>(), typeof(double[][]) },
                };

                static IRuntimeConnector CreateInput<TValue>(PoolingStrategy strategy)
                {
                    var runtimeConnector = Substitute.For<IRuntimeConnector<TValue>>();
                    var connector = Substitute.For<IConnector<TValue>, IPoolable>();
                    ((IPoolable)connector).CrossSourcePoolingStrategy.Returns(strategy);
                    ((IRuntimeConnector)runtimeConnector).Connector.Returns(connector);
                    return runtimeConnector;
                }

                static IRuntimeConnector CreateOutput<TValue>()
                {
                    var runtimeConnector = Substitute.For<IRuntimeConnector<TValue>>();
                    var connector = Substitute.For<IConnector<TValue>>();
                    ((IRuntimeConnector)runtimeConnector).Connector.Returns(connector);
                    return runtimeConnector;
                }
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_CanDetermineLinkValueType))]
        public void CanDetermineLinkValueType_Connector(IRuntimeConnector connector, Type expected)
        {
            var determined = DataTypeCompatibilityValidator.DetermineLinkValueType(connector.Connector);

            determined.Should().BeSameAs(expected);
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_CanDetermineLinkValueType))]
        public void CanDetermineLinkValueType_RuntimeConnector(IRuntimeConnector connector, Type expected)
        {
            var determined = DataTypeCompatibilityValidator.DetermineLinkValueType(connector);

            determined.Should().BeSameAs(expected);
        }

        [Fact]
        public void ThrowsIfConnectorIsPoolableAndPoolingStrategyIsConcatButValueIsNoArray()
        {
            var connector = Substitute.For<IConnector<object>, IPoolable>();
            ((IPoolable)connector).CrossSourcePoolingStrategy.Returns(PoolingStrategy.Concat);

            Action action = () => DataTypeCompatibilityValidator.DetermineLinkValueType(connector);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage($"*{nameof(PoolingStrategy.Concat)}*array*");
        }
    }

    public class DetermineValueType
    {
        public static TheoryData<Type, Type> ArgumentsFor_CanDetermineValueType => new()
        {
            { typeof(IConnector<object>), typeof(object) },
            { typeof(IConnector<bool>), typeof(bool) },
            { typeof(IConnectorInputWithSecondType<bool, string>), typeof(string) },
            { typeof(IConnectorInputWithSecondType<int, bool>), typeof(bool) },
            { typeof(ILink<object>), typeof(object) },
            { typeof(ILink<bool>), typeof(bool) },
            { typeof(IInternalLink<object, bool, bool>), typeof(bool) },
            { typeof(IInternalLink<bool, int, int>), typeof(int) },
            { typeof(IExternalIncomingLink<object, object, object>), typeof(object) },
            { typeof(IExternalIncomingLink<bool, bool, bool>), typeof(bool) },
            { typeof(IInternalLink<object, bool, bool[]>), typeof(bool) },
            { typeof(IInternalLink<bool, int, int[]>), typeof(int) },
            { typeof(IExternalIncomingLink<object, object, object[]>), typeof(object) },
            { typeof(IExternalIncomingLink<bool, bool, bool[]>), typeof(bool) },
            { typeof(IExternalOutgoingLink<object>), typeof(object) },
            { typeof(IExternalOutgoingLink<bool>), typeof(bool) },
            { typeof(ILinkWithSecondType<bool, string>), typeof(string) },
            { typeof(ILinkWithSecondType<int, bool>), typeof(bool) },
            { typeof(IRuntimeLink<object>), typeof(object) },
            { typeof(IRuntimeLink<bool>), typeof(bool) },
            { typeof(IPersistentVariable<object>), typeof(object) },
            { typeof(IPersistentVariable<bool>), typeof(bool) },
            { typeof(IPersistentVariableWithSecondType<bool, string>), typeof(string) },
            { typeof(IPersistentVariableWithSecondType<int, bool>), typeof(bool) },
            { typeof(IRuntimeConnector<object>), typeof(object) },
            { typeof(IRuntimeConnector<bool>), typeof(bool) },
            { typeof(IRuntimeConnectorWithSecondType<bool, string>), typeof(string) },
            { typeof(IRuntimeConnectorWithSecondType<int, bool>), typeof(bool) },
            { typeof(IRuntimePersistentVariable<object>), typeof(object) },
            { typeof(IRuntimePersistentVariable<bool>), typeof(bool) },
            { typeof(IRuntimePersistentVariableWithSecondType<bool, string>), typeof(string) },
            { typeof(IRuntimePersistentVariableWithSecondType<int, bool>), typeof(bool) },
            { typeof(IRuntimeSetting<object>), typeof(object) },
            { typeof(IRuntimeSetting<bool>), typeof(bool) },
            { typeof(IRuntimeSettingWithSecondType<bool, string>), typeof(string) },
            { typeof(IRuntimeSettingWithSecondType<int, bool>), typeof(bool) },
            { typeof(ISetting<object>), typeof(object) },
            { typeof(ISetting<bool>), typeof(bool) },
            { typeof(ISettingWithSecondType<bool, string>), typeof(string) },
            { typeof(ISettingWithSecondType<int, bool>), typeof(bool) },
            { typeof(IAggregatingPooling<object>), typeof(object) },
            { typeof(IAggregatingPooling<bool>), typeof(bool) },
            { typeof(IAggregatingPoolingWithSecondType<bool, string>), typeof(string) },
            { typeof(IAggregatingPoolingWithSecondType<int, bool>), typeof(bool) },
            { typeof(IValidator<object>), typeof(object) },
            { typeof(IValidator<bool>), typeof(bool) },
            { typeof(IValidatorWithSecondType<bool, string>), typeof(string) },
            { typeof(IValidatorWithSecondType<int, bool>), typeof(bool)  },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_CanDetermineValueType))]
        public void CanDetermineValueType_Object(Type type, Type expected)
        {
            var obj = Substitute.For([type], []);

            var determined = DataTypeCompatibilityValidator.DetermineValueType(obj);

            determined.Should().BeSameAs(expected);
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_CanDetermineValueType))]
        public void CanDetermineValueType_Type(Type type, Type expected)
        {
            var determined = DataTypeCompatibilityValidator.DetermineValueType(type);

            determined.Should().BeSameAs(expected);
        }

        public static TheoryData<Type> ArgumentsFor_ThrowsIfValueTypeCannotBeDetermined => new()
        {
            { typeof(IConnector) },
            { typeof(ILink) },
            { typeof(ILinkWithDestination) },
            { typeof(ILinkWithSource) },
            { typeof(IInternalLink) },
            { typeof(IExternalIncomingLink) },
            { typeof(IExternalOutgoingLink) },
            { typeof(IRuntimeLink) },
            { typeof(IPersistentVariable) },
            { typeof(IRuntimeConnector) },
            { typeof(IRuntimePersistentVariable) },
            { typeof(IRuntimeSetting) },
            { typeof(ISetting) },
            { typeof(IAggregatingPooling) },
            { typeof(IValidator) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ThrowsIfValueTypeCannotBeDetermined))]
        public void ThrowsIfValueTypeCannotBeDetermined_Object(Type type)
        {
            var obj = Substitute.For([type], []);

            Action action = () => DataTypeCompatibilityValidator.DetermineValueType(obj);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*type*not*determine*");
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_ThrowsIfValueTypeCannotBeDetermined))]
        public void ThrowsIfValueTypeCannotBeDetermined_Type(Type type)
        {
            Action action = () => DataTypeCompatibilityValidator.DetermineValueType(type);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*type*not*determine*");
        }

        [Fact]
        public void ThrowsIfTypeProvidesMultipleValueTypes_Object()
        {
            Action action = () => DataTypeCompatibilityValidator.DetermineValueType(new TypeWithMultipleValueTypes());

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*multiple*value*type*");
        }

        [Fact]
        public void ThrowsIfTypeProvidesMultipleValueTypes_Type()
        {
            Action action = () => DataTypeCompatibilityValidator.DetermineValueType(typeof(TypeWithMultipleValueTypes));

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*multiple*value*type*");
        }

        public interface IConnectorInputWithSecondType<TSecond, TValue> : IConnector<TValue> { }
        public interface ILinkWithSecondType<TSecond, TValue> : ILink<TValue> { }
        public interface IPersistentVariableWithSecondType<TSecond, TValue> : IPersistentVariable<TValue> { }
        public interface IRuntimeConnectorWithSecondType<TSecond, TValue> : IRuntimeConnector<TValue> { }
        public interface IRuntimePersistentVariableWithSecondType<TSecond, TValue> : IRuntimePersistentVariable<TValue> { }
        public interface IRuntimeSettingWithSecondType<TSecond, TValue> : IRuntimeSetting<TValue> { }
        public interface ISettingWithSecondType<TSecond, TValue> : ISetting<TValue> { }
        public interface IAggregatingPoolingWithSecondType<TSecond, TValue> : IAggregatingPooling<TValue> { }
        public interface IValidatorWithSecondType<TSecond, TValue> : IValidator<TValue> { }
    }

    public class DetermineValueTypes
    {
        public static TheoryData<Type, Type[]> ArgumentsFor_ReturnsAllDeterminedValueTypes => new()
        {
            { typeof(IConnector<object>), new[] { typeof(object) } },
            { typeof(TypeWithMultipleValueTypes), new[] { typeof(double), typeof(long) } },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ReturnsAllDeterminedValueTypes))]
        public void ReturnsAllDeterminedValueTypes_Object(Type type, Type[] expected)
        {
            var obj = Substitute.For([type], []);

            var valueTypes = DataTypeCompatibilityValidator.DetermineValueTypes(obj);

            valueTypes.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_ReturnsAllDeterminedValueTypes))]
        public void ReturnsAllDeterminedValueTypes_Type(Type type, Type[] expected)
        {
            var valueTypes = DataTypeCompatibilityValidator.DetermineValueTypes(type);

            valueTypes.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ReturnsEmptyEnumerationIfValueTypeCannotBeDetermined_Object()
        {
            var obj = Substitute.For([typeof(IConnector)], []);

            var valueTypes = DataTypeCompatibilityValidator.DetermineValueTypes(obj);

            valueTypes.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsEmptyEnumerationIfValueTypeCannotBeDetermined_Type()
        {
            var valueTypes = DataTypeCompatibilityValidator.DetermineValueTypes(typeof(IConnector));

            valueTypes.Should().BeEmpty();
        }

    }

    public class ValidateCompatibility
    {
        [Fact]
        public void ThrowsIfTypeConversionIsMissingButNecessary()
        {
            Action action = () => DataTypeCompatibilityValidator.ValidateCompatibility<int, bool>(null);

            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("*conversion*not*null*");
        }

        public static TheoryData<Type, Type> ArgumentsFor_DoesntThrowIfTypeConversionIsMissingAndNotNecessary => new()
        {
            { typeof(int), typeof(int) },
            { typeof(int), typeof(object) },
            { typeof(DateTime), typeof(object) },
            { typeof(DerivedType), typeof(BaseType) },
            { typeof(DerivedType), typeof(object) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_DoesntThrowIfTypeConversionIsMissingAndNotNecessary))]
        public void DoesntThrowIfTypeConversionIsMissingAndNotNecessary(Type sourceType, Type destinationType)
        {
            Action action = () => DataTypeCompatibilityValidator.ValidateCompatibility(null, sourceType, destinationType);

            action.Should().NotThrow();
        }

        [Fact]
        public void ThrowsIfTypeConverterCantConvert()
        {
            var conversion = Substitute.For<ITypeConversion>();
            conversion.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));

            Action action = () => DataTypeCompatibilityValidator.ValidateCompatibility<int, decimal>(conversion);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*unable*convert*");
        }

        [Fact]
        public void DoesntThrowIfTypeConverterCanConvert()
        {
            var conversion = Substitute.For<ITypeConversion>();
            conversion.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));

            Action action = () => DataTypeCompatibilityValidator.ValidateCompatibility<int, byte>(conversion);

            action.Should().NotThrow();
        }
    }

    public class TypeWithMultipleValueTypes : IAggregatingPooling<double>, IAggregatingPooling<long>
    {
        public string Description => string.Empty;
        public string Name => string.Empty;
        public double Aggregate(IEnumerable<double> values) => throw new NotImplementedException();
        public long Aggregate(IEnumerable<long> values) => throw new NotImplementedException();
    }

    private class BaseType { }
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "The type is used as a Theory argument.")]
    private sealed class DerivedType : BaseType { }
}
