using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class MinPoolingTests
{
    public class Aggregate_Double
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1.0 }, 1.0)]
        [InlineData(new[] { 1.5 }, 1.5)]
        [InlineData(new[] { 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123 }, 123456.123)]
        [InlineData(new[] { 1.0, 2.0 }, 1.0)]
        [InlineData(new[] { 1.5, 0.0 }, 0.0)]
        [InlineData(new[] { 10.0, 1.0 }, 1.0)]
        [InlineData(new[] { 10.0, 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123, 654321.321 }, 123456.123)]
        [InlineData(new[] { 1.0, 2.0, 3.0 }, 1.0)]
        [InlineData(new[] { 1.5, 0.0, 0.0 }, 0.0)]
        [InlineData(new[] { 10.0, 1.0, 0.0 }, 0.0)]
        [InlineData(new[] { 10.0, 10.0, 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123, 654321.321, 987654.321 }, 123456.123)]
        public void AggregatesCorrect(double[] inputValues, double expected)
        {
            var pooling = new MinPooling<double>();

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
        [InlineData(new[] { true, false }, false)]
        [InlineData(new[] { false, true }, false)]
        [InlineData(new[] { true, true }, true)]
        [InlineData(new[] { false, false, false }, false)]
        [InlineData(new[] { true, false, true }, false)]
        [InlineData(new[] { false, true, false }, false)]
        [InlineData(new[] { true, false, false }, false)]
        [InlineData(new[] { true, true, false }, false)]
        [InlineData(new[] { true, true, true }, true)]
        [InlineData(new[] { false, true, true }, false)]
        [InlineData(new[] { false, false, true }, false)]
        public void AggregatesCorrect(bool[] inputValues, bool expected)
        {
            var pooling = new MinPooling<bool>();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_String
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { "Eins" }, "Eins")]
        [InlineData(new[] { "Zwei" }, "Zwei")]
        [InlineData(new[] { "Eins", "Zwei" }, "Eins")]
        [InlineData(new[] { "Elf", "Fünf" }, "Elf")]
        [InlineData(new[] { "Elf", null }, "Elf")]
        [InlineData(new[] { null, "Fünf" }, "Fünf")]
        [InlineData(new[] { "Eins", "Zwei", "Sechs" }, "Eins")]
        [InlineData(new[] { "Elf", "Zweiundzwanzig", "Dreißig" }, "Dreißig")]
        [InlineData(new[] { "Elf", null, "Dreißig" }, "Dreißig")]
        [InlineData(new string?[] { null }, null)]
        [InlineData(new string?[] { null, null }, null)]
        [InlineData(new string?[] { null, null, null }, null)]
        public void AggregatesCorrect(string[] inputValues, string? expected)
        {
            var pooling = new MinPooling<string>();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
