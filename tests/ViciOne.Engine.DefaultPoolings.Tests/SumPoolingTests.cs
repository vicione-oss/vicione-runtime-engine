using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class SumPoolingTests
{
    public class Aggregate_Double
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1.0 }, 1.0)]
        [InlineData(new[] { 1.5 }, 1.5)]
        [InlineData(new[] { 10.0 }, 10.0)]
        [InlineData(new[] { 123456.123 }, 123456.123)]
        [InlineData(new[] { 1.0, 2.0 }, 3.0)]
        [InlineData(new[] { 1.5, 0.0 }, 1.5)]
        [InlineData(new[] { 10.0, 1.0 }, 11.0)]
        [InlineData(new[] { 10.0, 10.0 }, 20.0)]
        [InlineData(new[] { 123456.123, 654321.321 }, 777777.444)]
        [InlineData(new[] { 1.0, 2.0, 3.0 }, 6.0)]
        [InlineData(new[] { 1.5, 0.0, 0.0 }, 1.5)]
        [InlineData(new[] { 10.0, 1.0, 0.0 }, 11.0)]
        [InlineData(new[] { 10.0, 10.0, 10.0 }, 30.0)]
        [InlineData(new[] { 12345.123, 65432.321, 98765.321 }, 176542.765)]
        public void AggregatesCorrect(double[] inputValues, double expected)
        {
            var pooling = new SumPooling();

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
        [InlineData(new long[] { 1, 2 }, 3)]
        [InlineData(new long[] { 1, 0 }, 1)]
        [InlineData(new long[] { 10, 1 }, 11)]
        [InlineData(new long[] { 10, 10 }, 20)]
        [InlineData(new long[] { 123456, 654321 }, 777777)]
        [InlineData(new long[] { 1, 2, 3 }, 6)]
        [InlineData(new long[] { 1, 0, 0 }, 1)]
        [InlineData(new long[] { 10, 1, 0 }, 11)]
        [InlineData(new long[] { 10, 10, 10 }, 30)]
        [InlineData(new long[] { 123456, 654321, 987654 }, 1765431)]
        public void AggregatesCorrect(long[] inputValues, long expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
