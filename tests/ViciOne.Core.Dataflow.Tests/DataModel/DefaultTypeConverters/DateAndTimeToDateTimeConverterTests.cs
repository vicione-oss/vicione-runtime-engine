using AwesomeAssertions;
using System;
using System.Globalization;
using ViciOne.Core.Dataflow.DataModel.DefaultTypeConverters;
using Xunit;

public class DateAndTimeToDateTimeConverterTests
{
    private DateAndTimeToDateTimeConverter Converter { get; } = new();

    public class CanConvertFrom : DateAndTimeToDateTimeConverterTests
    {
        [Fact]
        public void ReturnsTrueForDateOnly()
        {
            var result = Converter.CanConvertFrom(null, typeof(DateOnly));

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsTrueForTimeOnly()
        {
            var result = Converter.CanConvertFrom(null, typeof(TimeOnly));

            result.Should().BeTrue();
        }

        [InlineData(typeof(string), true)]
        [InlineData(typeof(double), false)]
        [Theory]
        public void CallsBaseForOthers(Type sourceType, bool expectedResult)
        {
            var result = Converter.CanConvertFrom(null, sourceType);

            result.Should().Be(expectedResult);
        }
    }

    public class ConvertFrom : DateAndTimeToDateTimeConverterTests
    {
        [Fact]
        public void ConvertsFromDateOnly()
        {
            DateOnly value = new(2024, 02, 12);
            DateTime expectedDateTimePart = new(value.Year, value.Month, value.Day);

            var result = Converter.ConvertFrom(null, null, value);

            result.Should()
                .BeOfType<DateTime>().Which.Date.Should()
                    .Be(expectedDateTimePart);
        }

        [Fact]
        public void ConvertsFromTimeOnly()
        {
            TimeOnly value = new(12, 22, 42);
            TimeSpan expectedTimePart = new(value.Hour, value.Minute, value.Second);

            var result = Converter.ConvertFrom(null, null, value);

            result.Should()
                .BeOfType<DateTime>().Which.TimeOfDay.Should()
                    .Be(expectedTimePart);
        }

        [Fact]
        public void CallsBaseForOthers()
        {
            DateTime value = new(2024, 02, 12, 12, 22, 42);

            var result = Converter.ConvertFrom(null, CultureInfo.InvariantCulture, value.ToString(CultureInfo.InvariantCulture));

            result.Should()
                .BeOfType<DateTime>().Which.Should()
                    .Be(value);
        }
    }
}
