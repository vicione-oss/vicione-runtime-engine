using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;

namespace ViciOne.Engine.DefaultPoolings;

[DefaultPoolingForType<float>]
[DefaultPoolingForType<double>]
[DefaultPoolingForType<decimal>]
[DefaultPoolingForType<sbyte>]
[DefaultPoolingForType<byte>]
[DefaultPoolingForType<short>]
[DefaultPoolingForType<ushort>]
[DefaultPoolingForType<int>]
[DefaultPoolingForType<uint>]
[DefaultPoolingForType<long>]
[DefaultPoolingForType<ulong>]
public class AveragePooling :
    IAggregatingPooling<float>,
    IAggregatingPooling<double>,
    IAggregatingPooling<decimal>,
    IAggregatingPooling<sbyte>,
    IAggregatingPooling<byte>,
    IAggregatingPooling<short>,
    IAggregatingPooling<ushort>,
    IAggregatingPooling<int>,
    IAggregatingPooling<uint>,
    IAggregatingPooling<long>,
    IAggregatingPooling<ulong>
{
    public string Description => "Calculates average value of all current input values.";
    public string Name => "Average";

    public float Aggregate(IEnumerable<float> values)
        => values.Average();

    public double Aggregate(IEnumerable<double> values)
        => values.Average();

    public decimal Aggregate(IEnumerable<decimal> values)
        => values.Average();

    // Enumerable.Average has no overload for the integer types below, so their values are summed up in a wider
    // integer type, which keeps the result exact, and are rounded to even, just like Math.Round does.
    public sbyte Aggregate(IEnumerable<sbyte> values)
        => (sbyte)Average<sbyte, long>(values);

    public byte Aggregate(IEnumerable<byte> values)
        => (byte)Average<byte, long>(values);

    public short Aggregate(IEnumerable<short> values)
        => (short)Average<short, long>(values);

    public ushort Aggregate(IEnumerable<ushort> values)
        => (ushort)Average<ushort, long>(values);

    // int keeps using Enumerable.Average, because its span based summation beats the loop below and
    // because summing int values in a double is exact for any sequence that fits into memory. LINQ
    // vectorizes that summation only from Vector<int>.Count values on, which the few links of a
    // connector rarely reach, so the gain at the usual sizes comes from the span, not from SIMD.
    public int Aggregate(IEnumerable<int> values)
        => (int)Math.Round(values.Average());

    public uint Aggregate(IEnumerable<uint> values)
        => (uint)Average<uint, long>(values);

    // Enumerable.Average sums long values up in a long, which throws on overflow, and calculates the average
    // as a double, which is imprecise beyond 2^53. The values are therefore summed up in a 128 bit integer
    // and are rounded to even, just like Math.Round does. ulong needs the same treatment.
    public long Aggregate(IEnumerable<long> values)
        => (long)Average<long, Int128>(values);

    public ulong Aggregate(IEnumerable<ulong> values)
        => (ulong)Average<ulong, UInt128>(values);

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
