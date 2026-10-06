using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

/// <summary>
/// Several poolings deliberately do not use the LINQ call one would expect, because it allocates,
/// overflows or loses precision. These tests pin their results to a reference implementation that is
/// obviously correct but unsuitable for the hot path, using randomly generated input values.
/// The seeds are fixed, so a failure is always reproducible.
///
/// Every round is aggregated in all input shapes a pooling can see: an array and a list, which are
/// what the engine passes, and a lazy sequence, which is what reaches the fallback loop.
/// </summary>
[SuppressMessage("Security", "CA5394:Do not use insecure randomness",
    Justification = "The values are test data and the seeds are fixed for reproducibility.")]
public class PoolingEquivalenceTests
{
    private const int Rounds = 5000;

    public class Average_Long
    {
        // decimal represents every long value exactly and is wide enough to hold the sum,
        // which makes it the reference for the Int128 arithmetic of the pooling
        private static long AverageViaDecimal(IEnumerable<long> values)
            => (long)Math.Round(values.Average(value => (decimal)value));

        [Fact]
        public void MatchesTheDecimalReferenceForRandomValues()
        {
            var pooling = new AveragePooling();
            var random = new Random(20260922);

            for (int round = 0; round < Rounds; round++)
            {
                long[] values = CreateValues(random, () => random.NextInt64(long.MinValue, long.MaxValue));

                foreach (IEnumerable<long> shape in InputShapes(values))
                    pooling.Aggregate(shape).Should().Be(AverageViaDecimal(values), BecauseOf(values));
            }
        }

        [Fact]
        public void MatchesTheDecimalReferenceForSmallValues()
        {
            var pooling = new AveragePooling();
            var random = new Random(4711);

            // small values also cover the rounding of exact halves, which random 64 bit values hardly ever hit
            for (int round = 0; round < Rounds; round++)
            {
                long[] values = CreateValues(random, () => random.NextInt64(-10, 11));

                foreach (IEnumerable<long> shape in InputShapes(values))
                    pooling.Aggregate(shape).Should().Be(AverageViaDecimal(values), BecauseOf(values));
            }
        }
    }

    public class Sum_Short
    {
        // long is wide enough to hold the sum of any short sequence, which makes Enumerable.Sum
        // over long the reference for the generic accumulation of the pooling
        private static short SumViaLong(IEnumerable<short> values)
            => (short)values.Sum(value => (long)value);

        [Fact]
        public void MatchesTheLongReferenceForRandomValues()
        {
            var pooling = new SumPooling();
            var random = new Random(31415);

            for (int round = 0; round < Rounds; round++)
            {
                // the sum of at most eight values of this range still fits into a short, so the
                // pooling returns a result instead of throwing
                short[] values = CreateValues(random, () => (short)random.Next(-4096, 4096));

                foreach (IEnumerable<short> shape in InputShapes(values))
                    pooling.Aggregate(shape).Should().Be(SumViaLong(values), BecauseOf(values));
            }
        }
    }

    public class Sum_ULong
    {
        // decimal represents every ulong value exactly and is wide enough to hold the sum,
        // which makes it the reference for the UInt128 arithmetic of the pooling
        private static ulong SumViaDecimal(IEnumerable<ulong> values)
            => (ulong)values.Aggregate(0m, (sum, value) => sum + value);

        [Fact]
        public void MatchesTheDecimalReferenceForRandomValues()
        {
            var pooling = new SumPooling();
            var random = new Random(27182);

            for (int round = 0; round < Rounds; round++)
            {
                // the sum of at most eight values of this range still fits into a ulong, so the
                // pooling returns a result instead of throwing
                ulong[] values = CreateValues(random, () => (ulong)random.NextInt64(0, (long)(ulong.MaxValue / 8)));

                foreach (IEnumerable<ulong> shape in InputShapes(values))
                    pooling.Aggregate(shape).Should().Be(SumViaDecimal(values), BecauseOf(values));
            }
        }
    }

    public class Max_Double
    {
        [Fact]
        public void MatchesTheLinqReferenceForRandomValues()
        {
            var pooling = new MaxPooling<double>();
            var random = new Random(1234);

            for (int round = 0; round < Rounds; round++)
            {
                double[] values = CreateValues(random, () => NextSpecialDouble(random));

                foreach (IEnumerable<double> shape in InputShapes(values))
                    pooling.Aggregate(shape).Should().Be(values.Max(), BecauseOf(values));
            }
        }
    }

    public class Min_Double
    {
        [Fact]
        public void MatchesTheLinqReferenceForRandomValues()
        {
            var pooling = new MinPooling<double>();
            var random = new Random(1234);

            for (int round = 0; round < Rounds; round++)
            {
                double[] values = CreateValues(random, () => NextSpecialDouble(random));

                foreach (IEnumerable<double> shape in InputShapes(values))
                    pooling.Aggregate(shape).Should().Be(values.Min(), BecauseOf(values));
            }
        }
    }

    public class Max_String
    {
        [Fact]
        public void MatchesTheLinqReferenceForRandomValuesIncludingNull()
        {
            var pooling = new MaxPooling<string>();
            var random = new Random(5678);

            for (int round = 0; round < Rounds; round++)
            {
                string?[] values = CreateValues(random, () => random.Next(4) == 0 ? null : $"value{random.Next(50)}");

                foreach (IEnumerable<string?> shape in InputShapes(values))
                    pooling.Aggregate(shape!).Should().Be(values.Max(), BecauseOf(values));
            }
        }
    }

    public class Min_String
    {
        [Fact]
        public void MatchesTheLinqReferenceForRandomValuesIncludingNull()
        {
            var pooling = new MinPooling<string>();
            var random = new Random(5678);

            for (int round = 0; round < Rounds; round++)
            {
                string?[] values = CreateValues(random, () => random.Next(4) == 0 ? null : $"value{random.Next(50)}");

                foreach (IEnumerable<string?> shape in InputShapes(values))
                    pooling.Aggregate(shape!).Should().Be(values.Min(), BecauseOf(values));
            }
        }
    }

    public class Concat
    {
        private static string ConcatViaStringBuilder(IEnumerable<string?> values)
        {
            var builder = new StringBuilder();
            foreach (string? value in values)
                builder.Append(value);
            return builder.ToString();
        }

        [Fact]
        public void MatchesTheStringBuilderReferenceForRandomValues()
        {
            var pooling = new ConcatPooling();
            var random = new Random(987654);

            for (int round = 0; round < Rounds; round++)
            {
                string?[] values = CreateValues(random, () => random.Next(4) == 0 ? null : $"value{random.Next(50)}");

                foreach (IEnumerable<string?> shape in InputShapes(values))
                    pooling.Aggregate(shape).Should().Be(ConcatViaStringBuilder(values), BecauseOf(values));
            }
        }
    }

    // The engine passes an array for the per source aggregation and a list for the cross source one.
    // Both take the span path of the poolings, through a different branch each, so both are needed.
    // The lazy sequence is neither, which is the only way to reach the fallback loop.
    private static IEnumerable<IEnumerable<T>> InputShapes<T>(T[] values)
    {
        yield return values;
        yield return new List<T>(values);
        yield return values.Select(value => value);
    }

    private static T[] CreateValues<T>(Random random, Func<T> createValue)
    {
        var values = new T[random.Next(1, 9)];
        for (int i = 0; i < values.Length; i++)
            values[i] = createValue();
        return values;
    }

    private static double NextSpecialDouble(Random random)
        => random.Next(6) switch
        {
            0 => double.NaN,
            1 => double.NegativeInfinity,
            2 => double.PositiveInfinity,
            _ => Math.Round((random.NextDouble() * 200) - 100, 3),
        };

    private static string BecauseOf<T>(T[] values)
        => $"the values are [{string.Join(", ", values)}]";
}
