using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;

namespace ViciOne.Engine.DefaultPoolings;

[DefaultPoolingForType<double>]
[DefaultPoolingForType<long>]
public class AveragePooling :
    IAggregatingPooling<double>,
    IAggregatingPooling<long>
{
    public string Description => "Calculates average value of all current input values.";
    public string Name => "Average";

    public double Aggregate(IEnumerable<double> values)
        => values.Average();

    // Enumerable.Average sums long values up in a long, which throws on overflow, and calculates the average
    // as a double, which is imprecise beyond 2^53. The values are therefore summed up in a 128 bit integer
    // and are rounded to even, just like Math.Round does.
    public long Aggregate(IEnumerable<long> values)
        => (long)Average<long, Int128>(values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TAccumulator Average<TValue, TAccumulator>(IEnumerable<TValue> values)
        where TValue : IBinaryInteger<TValue>
        where TAccumulator : IBinaryInteger<TAccumulator>
    {
        TAccumulator sum = TAccumulator.Zero;
        int count;

        if (PoolingValues.TryGetSpan(values, out ReadOnlySpan<TValue> span))
        {
            foreach (TValue value in span)
                sum += TAccumulator.CreateTruncating(value);
            count = span.Length;
        }
        else
        {
            count = 0;
            foreach (TValue value in values)
            {
                sum += TAccumulator.CreateTruncating(value);
                count++;
            }
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements.");

        return DivideRoundingToEven(sum, TAccumulator.CreateTruncating(count));
    }

    private static TAccumulator DivideRoundingToEven<TAccumulator>(TAccumulator sum, TAccumulator count)
        where TAccumulator : IBinaryInteger<TAccumulator>
    {
        TAccumulator quotient = sum / count;
        TAccumulator remainder = sum - (quotient * count);

        // integer division truncates towards zero, the remainder therefore has to be turned into a positive one
        if (remainder < TAccumulator.Zero)
        {
            quotient -= TAccumulator.One;
            remainder += count;
        }

        // round up if the remainder is greater than one half, or if it is exactly one half and the quotient is odd
        TAccumulator twiceTheRemainder = remainder + remainder;
        if (twiceTheRemainder > count ||
            (twiceTheRemainder == count && (quotient & TAccumulator.One) == TAccumulator.One))
            quotient += TAccumulator.One;

        return quotient;
    }
}
