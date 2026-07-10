// BenchmarkDotNet requires public benchmark classes and payload types.
// Disable CA1515 (prefer internal in application projects) for this file.
#pragma warning disable CA1515

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ViciOne.Core.Dataflow.Cloning;

namespace ViciOne.Core.Dataflow.Benchmarks;

/// <summary>
/// Compares DeepCloner (new) against FastCloner (old baseline) across representative
/// clone scenarios. Each category groups the two implementations so BenchmarkDotNet
/// emits a Ratio column.
///
/// Run with: dotnet run -c Release --project benchmarks/ViciOne.Core.Dataflow.Benchmarks
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DeepClonerBenchmarks
{
    private BenchFlatPoco _flatPoco = null!;
    private BenchDeepGraph _deepGraph = null!;
    private List<BenchFlatPoco> _list = null!;
    private Dictionary<string, BenchFlatPoco> _dictionary = null!;
    private BenchFlatPoco[] _array = null!;
    private BenchCyclicNode _cyclicGraph = null!;

    [GlobalSetup]
    public void Setup()
    {
        _flatPoco = new BenchFlatPoco { Id = 1, Name = "benchmark", Value = 3.14, Flag = true };

        _deepGraph = BuildDeepGraph(depth: 10);

        _list = [];
        for (int i = 0; i < 100; i++)
            _list.Add(new BenchFlatPoco { Id = i, Name = $"item-{i}", Value = i * 1.5, Flag = i % 2 == 0 });

        _dictionary = [];
        for (int i = 0; i < 100; i++)
            _dictionary[$"key-{i}"] = new BenchFlatPoco { Id = i, Name = $"val-{i}", Value = i, Flag = true };

        _array = new BenchFlatPoco[100];
        for (int i = 0; i < 100; i++)
            _array[i] = new BenchFlatPoco { Id = i, Name = $"arr-{i}", Value = i, Flag = true };

        _cyclicGraph = new BenchCyclicNode { Value = 1 };
        var b = new BenchCyclicNode { Value = 2, Next = _cyclicGraph };
        _cyclicGraph.Next = b; // A ↔ B cycle
    }

    // Small flat object — measures per-clone overhead and allocation on the hot path.
    [Benchmark(Baseline = true, Description = "FastCloner — flat object")]
    [BenchmarkCategory("Flat object")]
    public BenchFlatPoco? FCFlatPoco() => FastCloner.FastCloner.DeepClone(_flatPoco);

    [Benchmark(Description = "DeepCloner — flat object")]
    [BenchmarkCategory("Flat object")]
    public BenchFlatPoco? CloneFlatPoco() => DeepCloner.DeepClone(_flatPoco);

    // Nested object chain 10 levels deep — measures how cloning scales with graph depth.
    [Benchmark(Baseline = true, Description = "FastCloner — deep graph (depth 10)")]
    [BenchmarkCategory("Deep graph (depth 10)")]
    public BenchDeepGraph? FCDeepGraph() => FastCloner.FastCloner.DeepClone(_deepGraph);

    [Benchmark(Description = "DeepCloner — deep graph (depth 10)")]
    [BenchmarkCategory("Deep graph (depth 10)")]
    public BenchDeepGraph? CloneDeepGraph() => DeepCloner.DeepClone(_deepGraph);

    // List of 100 objects — measures collection cloning throughput.
    [Benchmark(Baseline = true, Description = "FastCloner — List of 100")]
    [BenchmarkCategory("List of 100")]
    public List<BenchFlatPoco>? FCList() => FastCloner.FastCloner.DeepClone(_list);

    [Benchmark(Description = "DeepCloner — List of 100")]
    [BenchmarkCategory("List of 100")]
    public List<BenchFlatPoco>? CloneList() => DeepCloner.DeepClone(_list);

    // Dictionary with 100 string-keyed entries — measures cloning of a hash-table structure.
    [Benchmark(Baseline = true, Description = "FastCloner — Dictionary of 100")]
    [BenchmarkCategory("Dictionary of 100")]
    public Dictionary<string, BenchFlatPoco>? FCDictionary() => FastCloner.FastCloner.DeepClone(_dictionary);

    [Benchmark(Description = "DeepCloner — Dictionary of 100")]
    [BenchmarkCategory("Dictionary of 100")]
    public Dictionary<string, BenchFlatPoco>? CloneDictionary() => DeepCloner.DeepClone(_dictionary);

    // Array of 100 objects — measures cloning of a raw array.
    [Benchmark(Baseline = true, Description = "FastCloner — array of 100")]
    [BenchmarkCategory("Array of 100")]
    public BenchFlatPoco[]? FCArray() => FastCloner.FastCloner.DeepClone(_array);

    [Benchmark(Description = "DeepCloner — array of 100")]
    [BenchmarkCategory("Array of 100")]
    public BenchFlatPoco[]? CloneArray() => DeepCloner.DeepClone(_array);

    // Two-node cycle (A→B→A) — measures the overhead of circular-reference tracking.
    [Benchmark(Baseline = true, Description = "FastCloner — cyclic graph")]
    [BenchmarkCategory("Cyclic graph")]
    public BenchCyclicNode? FCCyclicGraph() => FastCloner.FastCloner.DeepClone(_cyclicGraph);

    [Benchmark(Description = "DeepCloner — cyclic graph")]
    [BenchmarkCategory("Cyclic graph")]
    public BenchCyclicNode? CloneCyclicGraph() => DeepCloner.DeepClone(_cyclicGraph);

    // First clone of a brand-new type — measures one-time delegate-compilation cost.
    [Benchmark(Description = "DeepCloner — first clone of a new type (cold)")]
    [IterationCount(5)]
    [WarmupCount(1)]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public BenchColdWrapper? CloneCold() => DeepCloner.DeepClone(new BenchColdWrapper { Value = 42 });

    private static BenchDeepGraph BuildDeepGraph(int depth)
    {
        var node = new BenchDeepGraph { Value = depth };
        for (int i = depth - 1; i >= 0; i--)
            node = new BenchDeepGraph { Value = i, Child = node };
        return node;
    }
}

public sealed class BenchFlatPoco
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double Value { get; set; }
    public bool Flag { get; set; }
}

public sealed class BenchDeepGraph
{
    public int Value { get; set; }
    public BenchDeepGraph? Child { get; set; }
}

public sealed class BenchCyclicNode
{
    public int Value { get; set; }
    public BenchCyclicNode? Next { get; set; }
}

// Dedicated type for the cold-path benchmark; after the first run the delegate is cached.
public sealed class BenchColdWrapper
{
    public int Value { get; set; }
}
