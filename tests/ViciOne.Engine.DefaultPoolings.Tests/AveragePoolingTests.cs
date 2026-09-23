using AwesomeAssertions;
using System;
using System.Collections.Generic;
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

        [Fact]
        public void ThrowsForEmptyInputValues()
        {
            var pooling = new AveragePooling();

            Action action = () => pooling.Aggregate(Array.Empty<double>());

            action.Should().Throw<InvalidOperationException>();
        }
    }

    public class Aggregate_Long
    {
        [Fact]
        public void ThrowsForEmptyInputValues()
        {
            var pooling = new AveragePooling();

            Action action = () => pooling.Aggregate(Array.Empty<long>());

            action.Should().Throw<InvalidOperationException>();
        }

        // the input values are typed explicitly, because a collection expression targeting IEnumerable
        // creates a read-only wrapper instead of an array, which would not take the span path of the pooling
        [Fact]
        public void AggregatesWithoutOverflowingTheValueRange()
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(new long[] { long.MaxValue, long.MaxValue - 2 });

            result.Should().Be(long.MaxValue - 1);
        }

        [Fact]
        public void AggregatesWithoutOverflowingTheValueRangeIfValuesAreProvidedAsList()
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(new List<long> { long.MaxValue, long.MaxValue - 2 });

            result.Should().Be(long.MaxValue - 1);
        }

        // the sum of these two values is 2^54 + 6, which a double cannot store, so averaging through
        // a double returns 9007199254740996. Values whose sum a double happens to store exactly, for
        // example 2^54 + 4, would let such an implementation pass.
        [Fact]
        public void AggregatesValuesBeyondThePrecisionOfDouble()
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(new long[] { 9007199254740993L, 9007199254740997L });

            result.Should().Be(9007199254740995L);
        }

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
