using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ViciOne.Engine.DefaultPoolings;

internal static class PoolingValues
{
    /// <summary>
    /// Provides the values as a span, which avoids the allocation of an enumerator. The engine hands the values
    /// over as a list or an array, so the span is available for every call that comes from the engine itself.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSpan<TValue>(IEnumerable<TValue> values, out ReadOnlySpan<TValue> span)
    {
        switch (values)
        {
            case TValue[] array:
                span = array;
                return true;
            case List<TValue> list:
                span = CollectionsMarshal.AsSpan(list);
                return true;
            default:
                span = default;
                return false;
        }
    }

    /// <summary>
    /// Returns the greatest value, behaving exactly like <see cref="System.Linq.Enumerable.Max{TSource}(IEnumerable{TSource})"/>:
    /// null values are skipped, and an empty sequence results in null for reference types, but throws for value types.
    /// </summary>
    public static TValue? GetGreatest<TValue>(ReadOnlySpan<TValue?> values)
        => GetExtreme(values, greatest: true);

    /// <summary>
    /// Returns the smallest value, behaving exactly like <see cref="System.Linq.Enumerable.Min{TSource}(IEnumerable{TSource})"/>:
    /// null values are skipped, and an empty sequence results in null for reference types, but throws for value types.
    /// </summary>
    public static TValue? GetSmallest<TValue>(ReadOnlySpan<TValue?> values)
        => GetExtreme(values, greatest: false);

    private static TValue? GetExtreme<TValue>(ReadOnlySpan<TValue?> values, bool greatest)
    {
        Comparer<TValue?> comparer = Comparer<TValue?>.Default;
        TValue? result = default;
        bool foundValue = false;

        foreach (TValue? value in values)
        {
            if (value is null)
                continue;

            if (foundValue == false)
            {
                result = value;
                foundValue = true;
                continue;
            }

            int comparison = comparer.Compare(value, result);
            if (greatest ? comparison > 0 : comparison < 0)
                result = value;
        }

        // for value types an empty sequence has no result at all, just like Enumerable.Max and Enumerable.Min
        if (foundValue == false && default(TValue) is not null)
            throw new InvalidOperationException("Sequence contains no elements.");

        return result;
    }
}
