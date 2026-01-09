using AwesomeAssertions;
using System.ComponentModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class TypeConversionEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesConversionIfNotSupplied()
        {
            var converter = new BooleanConverter();

            var editor = new TypeConversionEditor(converter);

            editor.Conversion.Should().NotBeNull();
        }

        [Fact]
        public void SetsConverterIfConversionIsCreated()
        {
            var converter = new BooleanConverter();

            var editor = new TypeConversionEditor(converter);

            editor.Conversion.TypeConverter.Should().NotBeNull();
        }

        [Fact]
        public void StoresSuppliedConversion()
        {
            var conversion = new TypeConversion();

            var editor = new TypeConversionEditor(conversion);

            editor.Conversion.Should().BeSameAs(conversion);
        }
    }

    public class SetTypeConverter
    {
        [Fact]
        public void SetsTypeConverter()
        {
            var converterToSet = new DoubleConverter();
            var editor = new TypeConversionEditor(new BooleanConverter());

            editor.SetTypeConverter(converterToSet);

            editor.Conversion.TypeConverter.Should().BeSameAs(converterToSet);
        }
    }

    public class SetUseConvertFrom
    {
        [Fact]
        public void SetsUseConvertFrom()
        {
            var editor = new TypeConversionEditor(new BooleanConverter());

            editor.SetUseConvertFrom(true);

            editor.Conversion.UseConvertFrom.Should().BeTrue();
        }
    }
}
