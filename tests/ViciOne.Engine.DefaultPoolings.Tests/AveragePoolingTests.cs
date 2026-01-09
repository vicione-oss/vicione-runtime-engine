using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class AveragePoolingTests
{
    public class Aggregate_Double
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1.0 }, 1.0)]
        [InlineData(new[] { 1.5 }, 1.5)]
        [InlineData(new[] { 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123 }, 123456.123)]
        [InlineData(new[] { 1.0, 2.0 }, 1.5)]
        [InlineData(new[] { 1.5, 0.0 }, 0.75)]
        [InlineData(new[] { 10.0, 1.0 }, 5.5)]
        [InlineData(new[] { 10.0, 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123, 654321.321 }, 388888.722)]
        [InlineData(new[] { 1.0, 2.0, 3.0 }, 2.0)]
        [InlineData(new[] { 1.5, 0.0, 0.0 }, 0.5)]
        [InlineData(new[] { 10.0, 1.0, 0.0 }, 11.0 / 3)]
        [InlineData(new[] { 10.0, 10.0, 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123, 654321.321, 987654.321 }, 588477.255)]
        public void AggregatesCorrect(double[] inputValues, double expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_Long
    {
        [Theory(DisplayName = "")]
        [InlineData(new long[] { 1 }, 1)]
        [InlineData(new long[] { 10 }, 10)]
        [InlineData(new long[] { 123456 }, 123456)]
        [InlineData(new long[] { 1, 2 }, 2)]
        [InlineData(new long[] { 1, 0 }, 0)]
        [InlineData(new long[] { 10, 1 }, 6)]
        [InlineData(new long[] { 10, 10 }, 10)]
        [InlineData(new long[] { 123456, 654321 }, 388888)]
        [InlineData(new long[] { 1, 2, 3 }, 2)]
        [InlineData(new long[] { 1, 0, 0 }, 0)]
        [InlineData(new long[] { 10, 1, 0 }, 4)]
        [InlineData(new long[] { 10, 10, 10 }, 10)]
        [InlineData(new long[] { 123456, 654321, 987654 }, 588477)]
        public void AggregatesCorrect(long[] inputValues, long expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
