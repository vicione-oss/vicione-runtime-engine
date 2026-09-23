using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class SumPoolingTests
{
    public class Aggregate_Float
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1.0f }, 1.0f)]
        [InlineData(new[] { 1.5f }, 1.5f)]
        [InlineData(new[] { 1.0f, 2.0f }, 3.0f)]
        [InlineData(new[] { 1.5f, 0.0f }, 1.5f)]
        [InlineData(new[] { 10.0f, 1.0f, 0.0f }, 11.0f)]
        [InlineData(new[] { 10.0f, 10.0f, 10.0f }, 30.0f)]
        public void AggregatesCorrect(float[] inputValues, float expected)
        {
            var pooling = new SumPooling();

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

    public class Aggregate_Decimal
    {
        public static TheoryData<decimal[], decimal> ArgumentsFor_AggregatesCorrect => new()
        {
            { [1.0m], 1.0m },
            { [1.5m], 1.5m },
            { [1.0m, 2.0m], 3.0m },
            { [1.5m, 0.0m], 1.5m },
            { [123456.123m, 654321.321m], 777777.444m },
            { [10.0m, 1.0m, 0.0m], 11.0m },
            { [0.1m, 0.2m], 0.3m },
        };

        [Theory(DisplayName = "")]
        [MemberData(nameof(ArgumentsFor_AggregatesCorrect))]
        public void AggregatesCorrect(decimal[] inputValues, decimal expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }

    public class Aggregate_SByte
    {
        [Theory(DisplayName = "")]
        [InlineData(new sbyte[] { 1 }, 1)]
        [InlineData(new sbyte[] { 1, 2 }, 3)]
        [InlineData(new sbyte[] { -128, 127 }, -1)]
        [InlineData(new sbyte[] { -10, -20, -30 }, -60)]
        [InlineData(new sbyte[] { 10, 1, 0 }, 11)]
        public void AggregatesCorrect(sbyte[] inputValues, sbyte expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new sbyte[] { sbyte.MaxValue, sbyte.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }

    public class Aggregate_Byte
    {
        [Theory(DisplayName = "")]
        [InlineData(new byte[] { 1 }, 1)]
        [InlineData(new byte[] { 1, 2 }, 3)]
        [InlineData(new byte[] { 200, 55 }, 255)]
        [InlineData(new byte[] { 10, 1, 0 }, 11)]
        public void AggregatesCorrect(byte[] inputValues, byte expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new byte[] { byte.MaxValue, byte.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }

    public class Aggregate_Short
    {
        [Theory(DisplayName = "")]
        [InlineData(new short[] { 1 }, 1)]
        [InlineData(new short[] { 1, 2 }, 3)]
        [InlineData(new short[] { -32768, 32767 }, -1)]
        [InlineData(new short[] { 12345, 6789 }, 19134)]
        public void AggregatesCorrect(short[] inputValues, short expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new short[] { short.MaxValue, short.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }

    public class Aggregate_UShort
    {
        [Theory(DisplayName = "")]
        [InlineData(new ushort[] { 1 }, 1)]
        [InlineData(new ushort[] { 1, 2 }, 3)]
        [InlineData(new ushort[] { 65000, 535 }, 65535)]
        [InlineData(new ushort[] { 10, 1, 0 }, 11)]
        public void AggregatesCorrect(ushort[] inputValues, ushort expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new ushort[] { ushort.MaxValue, ushort.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }

    public class Aggregate_Int
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { 1 }, 1)]
        [InlineData(new[] { 1, 2 }, 3)]
        [InlineData(new[] { -2147483648, 2147483647 }, -1)]
        [InlineData(new[] { 123456, 654321, 987654 }, 1765431)]
        public void AggregatesCorrect(int[] inputValues, int expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new int[] { int.MaxValue, int.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }

    public class Aggregate_UInt
    {
        [Theory(DisplayName = "")]
        [InlineData(new uint[] { 1 }, 1u)]
        [InlineData(new uint[] { 1, 2 }, 3u)]
        [InlineData(new uint[] { 4294967290, 5 }, 4294967295u)]
        [InlineData(new uint[] { 10, 1, 0 }, 11u)]
        public void AggregatesCorrect(uint[] inputValues, uint expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new uint[] { uint.MaxValue, uint.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }

    public class Aggregate_Long
    {
        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new long[] { long.MaxValue, long.MaxValue });

            action.Should().Throw<OverflowException>();
        }

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

    public class Aggregate_ULong
    {
        [Theory(DisplayName = "")]
        [InlineData(new ulong[] { 1 }, 1ul)]
        [InlineData(new ulong[] { 1, 2 }, 3ul)]
        [InlineData(new ulong[] { 10, 1, 0 }, 11ul)]
        // values beyond the exact range of double must not lose precision
        [InlineData(new ulong[] { 9007199254740993, 1 }, 9007199254740994ul)]
        [InlineData(new ulong[] { 18446744073709551614, 1 }, 18446744073709551615ul)]
        public void AggregatesCorrect(ulong[] inputValues, ulong expected)
        {
            var pooling = new SumPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }

        [Fact]
        public void ThrowsIfSumExceedsTheValueRange()
        {
            var pooling = new SumPooling();

            Action action = () => pooling.Aggregate(new ulong[] { ulong.MaxValue, ulong.MaxValue });

            action.Should().Throw<OverflowException>();
        }
    }
}
