using AwesomeAssertions;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class InternalLinkEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesLinkIfNotSupplied()
        {
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<object>();

            var editor = new InternalLinkEditor<object, object>(source, destination);

            editor.Link.Should().NotBeNull();
            editor.Link.Source.Should().BeSameAs(source);
            editor.Link.Destination.Should().BeSameAs(destination);
        }

        [Fact]
        public void SetsNewIdIfLinkIsCreated()
        {
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<object>();

            var editor = new InternalLinkEditor<object, object>(source, destination);

            editor.Link.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsSourceIfLinkIsCreated()
        {
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<object>();

            var editor = new InternalLinkEditor<object, object>(source, destination);

            editor.Link.Source.Should().BeSameAs(source);
        }

        [Fact]
        public void SetsDestinationIfLinkIsCreated()
        {
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<object>();

            var editor = new InternalLinkEditor<object, object>(source, destination);

            editor.Link.Destination.Should().BeSameAs(destination);
        }

        [Fact]
        public void SetsTypeConversionIfSupplied()
        {
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<MockType>();
            var conversion = new TypeConversion() { TypeConverter = new MockTypeConverter() };

            var editor = new InternalLinkEditor<object, MockType>(source, destination, conversion);

            editor.Link.TypeConversion.Should().BeSameAs(conversion);
        }

        [Fact]
        public void DeterminesTypeConversionIfNotSupplied()
        {
            var source = new ConnectorOutput<double>();
            var destination = new ConnectorInput<bool>();

            var editor = new InternalLinkEditor<double, bool>(source, destination);

            editor.Link.TypeConversion.Should().NotBeNull();
            editor.Link.TypeConversion!.TypeConverter.Should().BeOfType<DoubleConverter>();
            editor.Link.TypeConversion!.UseConvertFrom.Should().BeFalse();
        }

        [Fact]
        public void ThrowsIfTypeConversionCannotBeDetermined()
        {
            var source = new ConnectorOutput<bool>();
            var destination = new ConnectorInput<int>();

            Action action = () => _ = new InternalLinkEditor<bool, int>(source, destination);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*determined*");
        }

        [Fact]
        public void ValidatesDestinationValueType()
        {
            var source = new ConnectorOutput<string>();
            var destination = new ConnectorInput<long>();

            Action action = () => _ = new InternalLinkEditor<string, int, long>(source, destination);

            action.Should()
                .Throw<Exception>()
                .Where(e => e.StackTrace!.Contains(nameof(LinkEditorValidations.ValidateDestinationValueType)));
        }

        [Fact]
        public void StoresSuppliedLink()
        {
            var link = new InternalLink<object, object, object>();

            var editor = new InternalLinkEditor<object, object>(link);

            editor.Link.Should().BeSameAs(link);
        }
    }

    public class SetTypeConversion
    {
        [Fact]
        public void SetsTypeConversion()
        {
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<MockType>();
            var editor = new InternalLinkEditor<object, MockType>(source, destination);
            var conversionToSet = new TypeConversion() { TypeConverter = new MockTypeConverter() };

            editor.SetTypeConversion(conversionToSet);

            editor.Link.TypeConversion.Should().BeSameAs(conversionToSet);
        }

        [Fact]
        public void ValidatesTypeConversion()
        {
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<MockType>();
            var editor = new InternalLinkEditor<object, MockType>(source, destination);
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
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "The type is used as a type argument.")]
    private sealed class MockType { }
    private sealed class MockTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => true;
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(MockType);
    }
}
