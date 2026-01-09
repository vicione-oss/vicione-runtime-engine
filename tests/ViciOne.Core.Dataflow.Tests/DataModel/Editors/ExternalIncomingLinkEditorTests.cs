using AwesomeAssertions;
using System;
using System.ComponentModel;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class ExternalIncomingLinkEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesLinkIfNotSupplied()
        {
            var destination = new ConnectorInput<object>();

            var editor = new ExternalIncomingLinkEditor<object>(destination);

            editor.Link.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfLinkIsCreated()
        {
            var destination = new ConnectorInput<object>();

            var editor = new ExternalIncomingLinkEditor<object>(destination);

            editor.Link.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsDestinationIfLinkIsCreated()
        {
            var destination = new ConnectorInput<object>();

            var editor = new ExternalIncomingLinkEditor<object>(destination);

            editor.Link.Destination.Should().BeSameAs(destination);
        }

        [Fact]
        public void SetsTypeConversionIfSupplied()
        {
            var destination = new ConnectorInput<MockType>();
            var conversion = new TypeConversion() { TypeConverter = new MockTypeConverter() };

            var editor = new ExternalIncomingLinkEditor<object, MockType>(destination, conversion);

            editor.Link.TypeConversion.Should().BeSameAs(conversion);
        }

        [Fact]
        public void DeterminesTypeConversionIfNotSuppliedUsingConvertTo()
        {
            var destination = new ConnectorInput<bool>();

            var editor = new ExternalIncomingLinkEditor<double, bool>(destination);

            editor.Link.TypeConversion.Should().NotBeNull();
            editor.Link.TypeConversion!.TypeConverter.Should().BeOfType<DoubleConverter>();
            editor.Link.TypeConversion!.UseConvertFrom.Should().BeFalse();
        }

        [Fact]
        public void DeterminesTypeConversionIfNotSuppliedUsingConvertFrom()
        {
            var destination = new ConnectorInput<int>();

            var editor = new ExternalIncomingLinkEditor<string, int>(destination);

            editor.Link.TypeConversion.Should().NotBeNull();
            editor.Link.TypeConversion!.TypeConverter.Should().BeOfType<Int32Converter>();
            editor.Link.TypeConversion!.UseConvertFrom.Should().BeTrue();
        }

        [Fact]
        public void SkipsTypeConversionIfNotNecessary()
        {
            var destination = new ConnectorInput<MockType>();
            var conversion = new TypeConversion() { TypeConverter = new MockTypeConverter() };

            var editor = new ExternalIncomingLinkEditor<MockType>(destination, conversion);

            editor.Link.TypeConversion.Should().BeNull();
        }

        [Fact]
        public void ThrowsIfTypeConversionCannotBeDetermined()
        {
            var destination = new ConnectorInput<int>();

            Action action = () => _ = new ExternalIncomingLinkEditor<bool, int>(destination);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*determined*");
        }

        [Fact]
        public void ValidatesDestinationValueType()
        {
            var destination = new ConnectorInput<long>();

            Action action = () => _ = new ExternalIncomingLinkEditor<int, int, long>(destination);

            action.Should()
                .Throw<Exception>()
                .Where(e => e.StackTrace!.Contains(nameof(LinkEditorValidations.ValidateDestinationValueType)));
        }

        [Fact]
        public void StoresSuppliedLink()
        {
            var link = new ExternalIncomingLink<object, object, object>();

            var editor = new ExternalIncomingLinkEditor<object>(link);

            editor.Link.Should().BeSameAs(link);
        }
    }

    public class SetDefaultValue
    {
        [Fact]
        public void SetsDefaultValue()
        {
            var link = new ExternalIncomingLink<int, int, int>();
            var editor = new ExternalIncomingLinkEditor<int>(link);
            var valueToSet = 42;

            editor.SetDefaultValue(valueToSet);

            link.DefaultValue.Should().Be(valueToSet);
        }
    }

    public class SetTypeConversion
    {
        [Fact]
        public void SetsTypeConversion()
        {
            var destination = new ConnectorInput<MockType>();
            var editor = new ExternalIncomingLinkEditor<object, MockType>(destination);
            var conversionToSet = new TypeConversion() { TypeConverter = new MockTypeConverter() };

            editor.SetTypeConversion(conversionToSet);

            editor.Link.TypeConversion.Should().BeSameAs(conversionToSet);
        }

        [Fact]
        public void ValidatesTypeConversion()
        {
            var destination = new ConnectorInput<MockType>();
            var editor = new ExternalIncomingLinkEditor<object, MockType>(destination);
            var conversionToSet = new TypeConversion() { TypeConverter = new OtherMockTypeConverter() };

            Action action = () => editor.SetTypeConversion(conversionToSet);

            action.Should().Throw<Exception>()
                .Which.StackTrace
                    .Should().ContainEquivalentOf(nameof(DataTypeCompatibilityValidator))
                    .And.ContainEquivalentOf(nameof(DataTypeCompatibilityValidator.ValidateCompatibility));
        }

        private sealed class OtherMockTypeConverter : TypeConverter { }
    }

    [TypeConverter(typeof(MockTypeConverter))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Nicht instanziierte interne Klassen vermeiden.", Justification = "Der Typ wird als Typargument verwendet.")]
    private sealed class MockType { }
    private sealed class MockTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => true;
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(MockType);
    }
}
