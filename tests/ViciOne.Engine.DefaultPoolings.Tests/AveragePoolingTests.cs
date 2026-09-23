using AwesomeAssertions;
using System;
using System.Collections.Generic;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class AveragePoolingTests
{
    public class Aggregate_Float
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1.0f }, 1.0f)]
        [InlineData(new[] { 1.5f }, 1.5f)]
        [InlineData(new[] { 123456.123f }, 123456.123f)]
        [InlineData(new[] { 1.0f, 2.0f }, 1.5f)]
        [InlineData(new[] { 1.5f, 0.0f }, 0.75f)]
        [InlineData(new[] { 10.0f, 1.0f }, 5.5f)]
        [InlineData(new[] { 10.0f, 10.0f }, 10.0f)]
        [InlineData(new[] { 1.0f, 2.0f, 3.0f }, 2.0f)]
        [InlineData(new[] { 1.5f, 0.0f, 0.0f }, 0.5f)]
        [InlineData(new[] { 10.0f, 1.0f, 0.0f }, 11.0f / 3)]
        [InlineData(new[] { 10.0f, 10.0f, 10.0f }, 10.0f)]
        public void AggregatesCorrect(float[] inputValues, float expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

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

    public class Aggregate_Decimal
    {
        public static TheoryData<decimal[], decimal> ArgumentsFor_AggregatesCorrect => new()
        {
            { [1.0m], 1.0m },
            { [1.5m], 1.5m },
            { [123456.123m], 123456.123m },
            { [1.0m, 2.0m], 1.5m },
            { [1.5m, 0.0m], 0.75m },
            { [10.0m, 1.0m], 5.5m },
            { [10.0m, 10.0m], 10.0m },
            { [123456.123m, 654321.321m], 388888.722m },
            { [1.0m, 2.0m, 3.0m], 2.0m },
            { [1.5m, 0.0m, 0.0m], 0.5m },
            { [10.0m, 10.0m, 10.0m], 10.0m },
            // decimal keeps the full precision of values that double cannot represent exactly
            { [0.1m, 0.2m], 0.15m },
            { [7922816251426433759354395033.5m, 7922816251426433759354395033.5m], 7922816251426433759354395033.5m },
        };

        [Theory(DisplayName = "")]
        [MemberData(nameof(ArgumentsFor_AggregatesCorrect))]
        public void AggregatesCorrect(decimal[] inputValues, decimal expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumOfValuesExceedsValueRange()
        {
            var pooling = new AveragePooling();

            Action action = () => pooling.Aggregate(new[] { decimal.MaxValue, decimal.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }

    public class Aggregate_SByte
    {
        [Theory(DisplayName = "")]
        [InlineData(new sbyte[] { 1 }, 1)]
        [InlineData(new sbyte[] { -128 }, -128)]
        [InlineData(new sbyte[] { 127 }, 127)]
        [InlineData(new sbyte[] { 1, 2 }, 2)]
        [InlineData(new sbyte[] { 1, 0 }, 0)]
        [InlineData(new sbyte[] { 10, 1 }, 6)]
        [InlineData(new sbyte[] { -128, 127 }, 0)]
        [InlineData(new sbyte[] { 10, 1, 0 }, 4)]
        public void AggregatesCorrect(sbyte[] inputValues, sbyte expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_Byte
    {
        [Theory(DisplayName = "")]
        [InlineData(new byte[] { 1 }, 1)]
        [InlineData(new byte[] { 255 }, 255)]
        [InlineData(new byte[] { 1, 2 }, 2)]
        [InlineData(new byte[] { 1, 0 }, 0)]
        [InlineData(new byte[] { 10, 1 }, 6)]
        [InlineData(new byte[] { 255, 255 }, 255)]
        [InlineData(new byte[] { 10, 1, 0 }, 4)]
        public void AggregatesCorrect(byte[] inputValues, byte expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_Short
    {
        [Theory(DisplayName = "")]
        [InlineData(new short[] { 1 }, 1)]
        [InlineData(new short[] { -32768 }, -32768)]
        [InlineData(new short[] { 32767 }, 32767)]
        [InlineData(new short[] { 1, 2 }, 2)]
        [InlineData(new short[] { 1, 0 }, 0)]
        [InlineData(new short[] { 10, 1 }, 6)]
        [InlineData(new short[] { 12345, 23456 }, 17900)]
        [InlineData(new short[] { 10, 1, 0 }, 4)]
        public void AggregatesCorrect(short[] inputValues, short expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_UShort
    {
        [Theory(DisplayName = "")]
        [InlineData(new ushort[] { 1 }, 1)]
        [InlineData(new ushort[] { 65535 }, 65535)]
        [InlineData(new ushort[] { 1, 2 }, 2)]
        [InlineData(new ushort[] { 1, 0 }, 0)]
        [InlineData(new ushort[] { 10, 1 }, 6)]
        [InlineData(new ushort[] { 65535, 65535 }, 65535)]
        [InlineData(new ushort[] { 10, 1, 0 }, 4)]
        public void AggregatesCorrect(ushort[] inputValues, ushort expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_Int
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1 }, 1)]
        [InlineData(new[] { -2147483648 }, -2147483648)]
        [InlineData(new[] { 2147483647 }, 2147483647)]
        [InlineData(new[] { 1, 2 }, 2)]
        [InlineData(new[] { 1, 0 }, 0)]
        [InlineData(new[] { 10, 1 }, 6)]
        [InlineData(new[] { -2147483648, 2147483647 }, 0)]
        [InlineData(new[] { 123456, 654321 }, 388888)]
        [InlineData(new[] { 1, 2, 3 }, 2)]
        [InlineData(new[] { 10, 1, 0 }, 4)]
        [InlineData(new[] { 123456, 654321, 987654 }, 588477)]
        public void AggregatesCorrect(int[] inputValues, int expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_UInt
    {
        [Theory(DisplayName = "")]
        [InlineData(new uint[] { 1 }, 1u)]
        [InlineData(new uint[] { 4294967295 }, 4294967295u)]
        [InlineData(new uint[] { 1, 2 }, 2u)]
        [InlineData(new uint[] { 1, 0 }, 0u)]
        [InlineData(new uint[] { 10, 1 }, 6u)]
        [InlineData(new uint[] { 4294967295, 4294967295 }, 4294967295u)]
        [InlineData(new uint[] { 10, 1, 0 }, 4u)]
        public void AggregatesCorrect(uint[] inputValues, uint expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
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

    public class Aggregate_ULong
    {
        [Theory(DisplayName = "")]
        [InlineData(new ulong[] { 1 }, 1ul)]
        [InlineData(new ulong[] { 18446744073709551615 }, 18446744073709551615ul)]
        [InlineData(new ulong[] { 1, 2 }, 2ul)]
        [InlineData(new ulong[] { 1, 0 }, 0ul)]
        [InlineData(new ulong[] { 10, 1 }, 6ul)]
        [InlineData(new ulong[] { 10, 1, 0 }, 4ul)]
        // values beyond the exact range of double must not lose precision
        [InlineData(new ulong[] { 18446744073709551615, 18446744073709551615 }, 18446744073709551615ul)]
        [InlineData(new ulong[] { 9007199254740993, 9007199254740993 }, 9007199254740993ul)]
        public void AggregatesCorrect(ulong[] inputValues, ulong expected)
        {
            var pooling = new AveragePooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
