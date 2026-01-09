using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class MaxPoolingTests
{
    public class Aggregate_Double
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1.0 }, 1.0)]
        [InlineData(new[] { 1.5 }, 1.5)]
        [InlineData(new[] { 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123 }, 123456.123)]
        [InlineData(new[] { 1.0, 2.0 }, 2.0)]
        [InlineData(new[] { 1.5, 0.0 }, 1.5)]
        [InlineData(new[] { 10.0, 1.0 }, 10.0)]
        [InlineData(new[] { 10.0, 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123, 654321.321 }, 654321.321)]
        [InlineData(new[] { 1.0, 2.0, 3.0 }, 3.0)]
        [InlineData(new[] { 1.5, 0.0, 0.0 }, 1.5)]
        [InlineData(new[] { 10.0, 1.0, 0.0 }, 10.0)]
        [InlineData(new[] { 10.0, 10.0, 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123, 654321.321, 987654.321 }, 987654.321)]
        public void AggregatesCorrect(double[] inputValues, double expected)
        {
            var pooling = new MaxPooling<double>();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_Bool
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { false }, false)]
        [InlineData(new[] { true }, true)]
        [InlineData(new[] { false, false }, false)]
        [InlineData(new[] { true, false }, true)]
        [InlineData(new[] { false, true }, true)]
        [InlineData(new[] { true, true }, true)]
        [InlineData(new[] { false, false, false }, false)]
        [InlineData(new[] { true, false, true }, true)]
        [InlineData(new[] { false, true, false }, true)]
        [InlineData(new[] { true, false, false }, true)]
        [InlineData(new[] { true, true, false }, true)]
        [InlineData(new[] { true, true, true }, true)]
        [InlineData(new[] { false, true, true }, true)]
        [InlineData(new[] { false, false, true }, true)]
        public void AggregatesCorrect(bool[] inputValues, bool expected)
        {
            var pooling = new MaxPooling<bool>();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_String
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { "Eins" }, "Eins")]
        [InlineData(new[] { "Zwei" }, "Zwei")]
        [InlineData(new[] { "Eins", "Zwei" }, "Zwei")]
        [InlineData(new[] { "Elf", "Fünf" }, "Fünf")]
        [InlineData(new[] { null, "Fünf" }, "Fünf")]
        [InlineData(new[] { "Elf", null }, "Elf")]
        [InlineData(new[] { "Eins", "Zwei", "Sechs" }, "Zwei")]
        [InlineData(new[] { "Elf", "Zweiundzwanzig", "Dreißig" }, "Zweiundzwanzig")]
        [InlineData(new[] { "Elf", null, "Dreißig" }, "Elf")]
        [InlineData(new string?[] { null }, null)]
        [InlineData(new string?[] { null, null }, null)]
        [InlineData(new string?[] { null, null, null }, null)]
        public void AggregatesCorrect(string[] inputValues, string? expected)
        {
            var pooling = new MaxPooling<string>();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void CanHandleEmptyInputValues()
        {
            var pooling = new MaxPooling<string>();

            Action action = () => pooling.Aggregate([]);

            action.Should().NotThrow();
        }
    }
}
