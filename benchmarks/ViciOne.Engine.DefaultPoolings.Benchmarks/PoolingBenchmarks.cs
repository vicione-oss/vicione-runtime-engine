// BenchmarkDotNet requires public benchmark classes and payload types.
// Disable CA1515 (prefer internal in application projects) for this file.
#pragma warning disable CA1515

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings.Benchmarks;

/// <summary>
/// Compares the default poolings whose implementation is not the obvious one against the alternative
/// one would otherwise write, because several of them deliberately do not use the LINQ call one would
/// expect, while others deliberately still do. Every category holds the current implementation and its
/// alternative, so BenchmarkDotNet emits a Ratio column.
///
/// NandPooling, NorPooling and XnorPooling are not covered. They aggregate like the poolings below
/// that are their counterpart, so a category of their own would measure the same thing.
///
/// The engine calls a pooling once per connector and cycle and hands the values over as a list or an
/// array, which is what the value counts and the input types below reproduce.
///
/// Run with: dotnet run -c Release --project benchmarks/ViciOne.Engine.DefaultPoolings.Benchmarks
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class PoolingBenchmarks
{
    private List<double> _doubles = null!;
    private List<int> _ints = null!;
    private List<ushort> _ushorts = null!;
    private List<long> _longs = null!;
    private List<bool> _allTrue = null!;
    private List<bool> _allFalse = null!;
    private List<string?> _strings = null!;

    private IAggregatingPooling<double> _averageDouble = null!;
    private IAggregatingPooling<int> _averageInt = null!;
    private IAggregatingPooling<ushort> _averageUShort = null!;
    private IAggregatingPooling<long> _averageLong = null!;
    private IAggregatingPooling<int> _sumInt = null!;
    private IAggregatingPooling<ushort> _sumUShort = null!;
    private IAggregatingPooling<double> _maxDouble = null!;
    private IAggregatingPooling<double> _minDouble = null!;
    private IAggregatingPooling<string> _maxString = null!;
    private IAggregatingPooling<bool> _and = null!;
    private IAggregatingPooling<bool> _or = null!;
    private IAggregatingPooling<bool> _xor = null!;
    private IAggregatingPooling<string> _concat = null!;

    /// <summary>The number of links that feed the connector.</summary>
    [Params(2, 4, 16)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _doubles = [.. Enumerable.Range(1, Count).Select(x => x * 1.5)];
        _ints = [.. Enumerable.Range(1, Count)];
        _ushorts = [.. Enumerable.Range(1, Count).Select(x => (ushort)x)];
        _longs = [.. Enumerable.Range(1, Count).Select(x => (long)x * 1_000_000_007)];
        // all equal, so that the early exit of All and Any does not hide the per value costs
        _allTrue = [.. Enumerable.Repeat(true, Count)];
        _allFalse = [.. Enumerable.Repeat(false, Count)];
        _strings = [.. Enumerable.Range(1, Count).Select(x => (string?)$"value{x}")];

        var average = new AveragePooling();
        _averageDouble = average;
        _averageInt = average;
        _averageUShort = average;
        _averageLong = average;

        var sum = new SumPooling();
        _sumInt = sum;
        _sumUShort = sum;
        _maxDouble = new MaxPooling<double>();
        _minDouble = new MinPooling<double>();
        _maxString = new MaxPooling<string>();
        _and = new AndPooling();
        _or = new OrPooling();
        _xor = new XorPooling();
        _concat = new ConcatPooling();
    }

    // Average of double stays on Enumerable.Average, which already reads a list or an array as a span
    // and allocates nothing. Its summation is not vectorized; LINQ sums double values one by one to
    // keep the result exact.

    [Benchmark(Baseline = true, Description = "AveragePooling — double")]
    [BenchmarkCategory("Average double")]
    public double AverageDouble() => _averageDouble.Aggregate(_doubles);

    [Benchmark(Description = "Summing loop — double")]
    [BenchmarkCategory("Average double")]
    public double AverageDoubleByLoop() => SumSpan(_doubles) / _doubles.Count;

    // Average of int stays on Enumerable.Average as well, although its sibling integer types do not.
    // Summing int values in a double is exact for any sequence that fits into memory, and the span
    // based summation of LINQ beats the loop that the other integer types have to use. Its vector
    // path needs Vector<int>.Count values, so at the counts below it is reached at most once.

    [Benchmark(Baseline = true, Description = "AveragePooling — int")]
    [BenchmarkCategory("Average int")]
    public int AverageInt() => _averageInt.Aggregate(_ints);

    [Benchmark(Description = "Summing loop — int")]
    [BenchmarkCategory("Average int")]
    public int AverageIntByLoop() => (int)AverageByLoop(_ints);

    // Average of ushort has no Enumerable.Average overload. Projecting to decimal is the obvious
    // alternative and keeps the result exact, but decimal is software arithmetic.

    [Benchmark(Baseline = true, Description = "AveragePooling — ushort")]
    [BenchmarkCategory("Average ushort")]
    public ushort AverageUShort() => _averageUShort.Aggregate(_ushorts);

    [Benchmark(Description = "Enumerable.Average over decimal — ushort")]
    [BenchmarkCategory("Average ushort")]
    public ushort AverageUShortByDecimal() => (ushort)Math.Round(_ushorts.Average(value => (decimal)value));

    // Sum follows the same split: int and long keep Enumerable.Sum, the other integer types are
    // summed up in a wider integer type. Enumerable.Sum vectorizes only from four vectors worth of
    // values on, so none of the counts below reaches it and the comparison is span against span.

    [Benchmark(Baseline = true, Description = "SumPooling — int")]
    [BenchmarkCategory("Sum int")]
    public int SumInt() => _sumInt.Aggregate(_ints);

    [Benchmark(Description = "Summing loop — int")]
    [BenchmarkCategory("Sum int")]
    public int SumIntByLoop() => checked((int)SumByLoop(_ints));

    [Benchmark(Baseline = true, Description = "SumPooling — ushort")]
    [BenchmarkCategory("Sum ushort")]
    public ushort SumUShort() => _sumUShort.Aggregate(_ushorts);

    [Benchmark(Description = "Enumerable.Sum over long — ushort")]
    [BenchmarkCategory("Sum ushort")]
    public ushort SumUShortByLinq() => checked((ushort)_ushorts.Sum(value => (long)value));

    // Average of long uses an Int128 accumulator instead of Enumerable.Average, which overflows
    // and loses precision. The alternative below is the faster but incorrect implementation.

    [Benchmark(Baseline = true, Description = "AveragePooling — long")]
    [BenchmarkCategory("Average long")]
    public long AverageLong() => _averageLong.Aggregate(_longs);

    [Benchmark(Description = "Enumerable.Average — long")]
    [BenchmarkCategory("Average long")]
    public long AverageLongByLinq() => (long)Math.Round(_longs.Average());

    // Max and Min read a span, because the generic Enumerable.Max has no span fast path
    // and boxes the enumerator of the list the engine passes in.

    [Benchmark(Baseline = true, Description = "MaxPooling — double")]
    [BenchmarkCategory("Max double")]
    public double MaxDouble() => _maxDouble.Aggregate(_doubles);

    [Benchmark(Description = "Generic Enumerable.Max — double")]
    [BenchmarkCategory("Max double")]
    public double MaxDoubleByLinq() => MaxViaGenericLinq(_doubles);

    [Benchmark(Baseline = true, Description = "MinPooling — double")]
    [BenchmarkCategory("Min double")]
    public double MinDouble() => _minDouble.Aggregate(_doubles);

    [Benchmark(Description = "Generic Enumerable.Min — double")]
    [BenchmarkCategory("Min double")]
    public double MinDoubleByLinq() => MinViaGenericLinq(_doubles);

    [Benchmark(Baseline = true, Description = "MaxPooling — string")]
    [BenchmarkCategory("Max string")]
    public string? MaxString() => _maxString.Aggregate(_strings!);

    [Benchmark(Description = "Generic Enumerable.Max — string")]
    [BenchmarkCategory("Max string")]
    public string? MaxStringByLinq() => MaxViaGenericLinq(_strings);

    // Concat uses string.Join, which reads a list or an array as a span and determines the total
    // length up front, so it allocates the result and nothing else.

    [Benchmark(Baseline = true, Description = "ConcatPooling")]
    [BenchmarkCategory("Concat")]
    public string? Concat() => _concat.Aggregate(_strings!);

    [Benchmark(Description = "StringBuilder")]
    [BenchmarkCategory("Concat")]
    public string ConcatByStringBuilder() => ConcatWithBuilder(_strings);

    // The boolean poolings stay on LINQ, whose All, Any and Aggregate are not slower than a loop here.

    [Benchmark(Baseline = true, Description = "AndPooling")]
    [BenchmarkCategory("And")]
    public bool And() => _and.Aggregate(_allTrue);

    [Benchmark(Description = "Loop over a span")]
    [BenchmarkCategory("And")]
    public bool AndByLoop() => AllTrue(_allTrue);

    [Benchmark(Baseline = true, Description = "OrPooling")]
    [BenchmarkCategory("Or")]
    public bool Or() => _or.Aggregate(_allFalse);

    [Benchmark(Description = "Loop over a span")]
    [BenchmarkCategory("Or")]
    public bool OrByLoop() => AnyTrue(_allFalse);

    [Benchmark(Baseline = true, Description = "XorPooling")]
    [BenchmarkCategory("Xor")]
    public bool Xor() => _xor.Aggregate(_allTrue);

    [Benchmark(Description = "Loop over a span")]
    [BenchmarkCategory("Xor")]
    public bool XorByLoop() => Parity(_allTrue);

    // ----- alternative implementations -----

    // binds to the generic Enumerable.Max, which is the overload a pooling with a generic value type gets
    private static T? MaxViaGenericLinq<T>(IEnumerable<T?> values) => values.Max();

    private static T? MinViaGenericLinq<T>(IEnumerable<T?> values) => values.Min();

    private static string ConcatWithBuilder(IEnumerable<string?> values)
    {
        var builder = new StringBuilder();
        foreach (string? value in values)
            builder.Append(value);
        return builder.ToString();
    }

    // sums up in a wider integer type and rounds to even, which is what the pooling does for the
    // integer types that have no Enumerable.Average overload
    private static long AverageByLoop(List<int> values)
    {
        long sum = SumByLoop(values);
        long quotient = sum / values.Count;
        long remainder = sum - (quotient * values.Count);
        if (remainder < 0)
        {
            quotient--;
            remainder += values.Count;
        }

        long twiceTheRemainder = remainder + remainder;
        if (twiceTheRemainder > values.Count ||
            (twiceTheRemainder == values.Count && (quotient & 1) == 1))
            quotient++;

        return quotient;
    }

    private static long SumByLoop(List<int> values)
    {
        long sum = 0;
        foreach (int value in CollectionsMarshal.AsSpan(values))
            sum += value;
        return sum;
    }

    private static double SumSpan(List<double> values)
    {
        double sum = 0;
        foreach (double value in CollectionsMarshal.AsSpan(values))
            sum += value;
        return sum;
    }

    private static bool AllTrue(List<bool> values)
    {
        foreach (bool value in CollectionsMarshal.AsSpan(values))
        {
            if (value == false)
                return false;
        }

        return true;
    }

    private static bool AnyTrue(List<bool> values)
    {
        foreach (bool value in CollectionsMarshal.AsSpan(values))
        {
            if (value)
                return true;
        }

        return false;
    }

    private static bool Parity(List<bool> values)
    {
        bool result = false;
        foreach (bool value in CollectionsMarshal.AsSpan(values))
            result ^= value;
        return result;
    }
}
