using AwesomeAssertions;
using System;
using System.ComponentModel;
using ViciOne.Core.Dataflow;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.DefaultTypeConverters;
using Xunit;

public class TypeConversionResolverTests
{
    public class DetermineDefaultTypeConversion
    {
        public static TheoryData<Type, Type> ArgumentsFor_ReturnsTrueAndNullIfNoConversionNecessary => new()
        {
            { typeof(MockType), typeof(MockType) },
            { typeof(int), typeof(object) },
            { typeof(DateTime), typeof(object) },
            { typeof(DerivedType), typeof(BaseType) },
            { typeof(DerivedType), typeof(object) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ReturnsTrueAndNullIfNoConversionNecessary))]
        public void ReturnsTrueAndNullIfNoConversionNecessary(Type sourceType, Type destinationType)
        {
            var result = TypeConversionResolver.DetermineDefaultTypeConversion(sourceType, destinationType, null, out var determinedTypeConversion);

            result.Should().BeTrue();
            determinedTypeConversion.Should().BeNull();
        }

        [Fact]
        public void ReturnsTrueAndSuppliedConversionIfSuppliedConversionIsValid()
        {
            var sourceType = typeof(object);
            var destinationType = typeof(MockType);
            var suppliedTypeConversion = new TypeConversion() { TypeConverter = new MockTypeConverter() };

            var result = TypeConversionResolver.DetermineDefaultTypeConversion(sourceType, destinationType, suppliedTypeConversion, out var determinedTypeConversion);

            result.Should().BeTrue();
            determinedTypeConversion.Should().BeSameAs(suppliedTypeConversion);
        }

        public static TheoryData<Type, Type, Type> ArgumentsFor_DeterminesTypeConversionIfNotSuppliedUsingConvertTo => new()
        {
            { typeof(sbyte), typeof(short), typeof(SByteConverter) },
            { typeof(byte), typeof(short), typeof(ByteConverter) },

            { typeof(sbyte), typeof(ushort), typeof(SByteConverter) },
            { typeof(byte), typeof(ushort), typeof(ByteConverter) },

            { typeof(sbyte), typeof(int), typeof(SByteConverter) },
            { typeof(byte), typeof(int), typeof(ByteConverter) },
            { typeof(short), typeof(int), typeof(Int16Converter) },
            { typeof(ushort), typeof(int), typeof(UInt16Converter) },

            { typeof(sbyte), typeof(uint), typeof(SByteConverter) },
            { typeof(byte), typeof(uint), typeof(ByteConverter) },
            { typeof(short), typeof(uint), typeof(Int16Converter) },
            { typeof(ushort), typeof(uint), typeof(UInt16Converter) },

            { typeof(sbyte), typeof(long), typeof(SByteConverter) },
            { typeof(byte), typeof(long), typeof(ByteConverter) },
            { typeof(short), typeof(long), typeof(Int16Converter) },
            { typeof(ushort), typeof(long), typeof(UInt16Converter) },
            { typeof(int), typeof(long), typeof(Int32Converter) },
            { typeof(uint), typeof(long), typeof(UInt32Converter) },

            { typeof(sbyte), typeof(ulong), typeof(SByteConverter) },
            { typeof(byte), typeof(ulong), typeof(ByteConverter) },
            { typeof(short), typeof(ulong), typeof(Int16Converter) },
            { typeof(ushort), typeof(ulong), typeof(UInt16Converter) },
            { typeof(int), typeof(ulong), typeof(Int32Converter) },
            { typeof(uint), typeof(ulong), typeof(UInt32Converter) },

            { typeof(float), typeof(double), typeof(SingleConverter) },

            // TOOD: should this be allowed?
            // TODO: no
            { typeof(double), typeof(bool), typeof(DoubleConverter) },
            { typeof(double), typeof(sbyte), typeof(DoubleConverter) },
            // ...
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_DeterminesTypeConversionIfNotSuppliedUsingConvertTo))]
        public void DeterminesTypeConversionIfNotSuppliedUsingConvertTo(Type sourceType, Type destinationType, Type expectedConverterType)
        {
            var result = TypeConversionResolver.DetermineDefaultTypeConversion(sourceType, destinationType, null, out var determinedTypeConversion);

            result.Should().BeTrue();
            determinedTypeConversion.Should().NotBeNull();
            determinedTypeConversion!.TypeConverter.Should().BeOfType(expectedConverterType);
            determinedTypeConversion!.UseConvertFrom.Should().BeFalse();
        }

        public static TheoryData<Type, Type, Type> ArgumentsFor_DeterminesTypeConversionIfNotSuppliedUsingConvertFrom => new()
        {
            { typeof(string), typeof(int), typeof(Int32Converter) },

            { typeof(DateOnly), typeof(DateTime), typeof(DateAndTimeToDateTimeConverter) },
            { typeof(TimeOnly), typeof(DateTime), typeof(DateAndTimeToDateTimeConverter) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_DeterminesTypeConversionIfNotSuppliedUsingConvertFrom))]
        public void DeterminesTypeConversionIfNotSuppliedUsingConvertFrom(Type sourceType, Type destinationType, Type expectedConverterType)
        {
            var result = TypeConversionResolver.DetermineDefaultTypeConversion(sourceType, destinationType, null, out var determinedTypeConversion);

            result.Should().BeTrue();
            determinedTypeConversion.Should().NotBeNull();
            determinedTypeConversion!.TypeConverter.Should().BeOfType(expectedConverterType);
            determinedTypeConversion!.UseConvertFrom.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfNoConversionAvailable()
        {
            var sourceType = typeof(bool);
            var destinationType = typeof(int);

            var result = TypeConversionResolver.DetermineDefaultTypeConversion(sourceType, destinationType, null, out _);

            result.Should().BeFalse();
        }
    }

    [TypeConverter(typeof(MockTypeConverter))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "The type is used as a Theory argument.")]
    private sealed class MockType { }
    private sealed class MockTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => true;
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(MockType);
    }

    private class BaseType { }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "The type is used as a Theory argument.")]
    private sealed class DerivedType : BaseType { }
}
