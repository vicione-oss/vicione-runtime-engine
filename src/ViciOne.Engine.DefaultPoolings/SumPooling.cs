using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class SumPooling :
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
    public string Description => "Calculates the sum of all current input values.";
    public string Name => "Sum";

    public float Aggregate(IEnumerable<float> values)
        => values.Sum();

    public double Aggregate(IEnumerable<double> values)
        => values.Sum();

    public decimal Aggregate(IEnumerable<decimal> values)
        => values.Sum();

    // Enumerable.Sum has no overload for the integer types below, so their values are summed up in a wider
    // integer type. Like Enumerable.Sum, they throw if the result exceeds the value range of the type.
    public sbyte Aggregate(IEnumerable<sbyte> values)
        => checked((sbyte)Sum<sbyte, long>(values));

    public byte Aggregate(IEnumerable<byte> values)
        => checked((byte)Sum<byte, long>(values));

    public short Aggregate(IEnumerable<short> values)
        => checked((short)Sum<short, long>(values));

    public ushort Aggregate(IEnumerable<ushort> values)
        => checked((ushort)Sum<ushort, long>(values));

    // int and long keep using Enumerable.Sum, whose span based summation beats the loop below and which
    // throws on overflow as well. LINQ vectorizes it only from four vectors worth of values on, which a
    // connector hardly ever reaches, so the gain at the usual sizes comes from the span, not from SIMD.
    public int Aggregate(IEnumerable<int> values)
        => values.Sum();

    public uint Aggregate(IEnumerable<uint> values)
        => checked((uint)Sum<uint, long>(values));

    public long Aggregate(IEnumerable<long> values)
        => values.Sum();

    // the sum of two ulong values can already exceed their range, so it is accumulated in a 128 bit integer
    public ulong Aggregate(IEnumerable<ulong> values)
        => checked((ulong)Sum<ulong, UInt128>(values));

    private static TAccumulator Sum<TValue, TAccumulator>(IEnumerable<TValue> values)
        where TValue : IBinaryInteger<TValue>
        where TAccumulator : IBinaryInteger<TAccumulator>
    {
        TAccumulator sum = TAccumulator.Zero;

        if (PoolingValues.TryGetSpan(values, out ReadOnlySpan<TValue> span))
        {
            foreach (TValue value in span)
                sum += TAccumulator.CreateTruncating(value);
        }
        else
        {
            foreach (TValue value in values)
                sum += TAccumulator.CreateTruncating(value);
        }

        return sum;
    }
}
