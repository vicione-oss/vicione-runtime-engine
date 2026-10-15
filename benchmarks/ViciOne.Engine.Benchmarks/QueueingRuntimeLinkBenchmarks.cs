// BenchmarkDotNet requires public benchmark classes and payload types.
// Disable CA1515 (prefer internal in application projects) for this file.
#pragma warning disable CA1515

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;

namespace ViciOne.Engine.Benchmarks;

/// <summary>
/// Measures the queue of a runtime link, which buffers the values of a single source until the
/// engine pools them.
///
/// WriteAndDrain writes the values one source delivers within a cycle and drains them like the
/// pooling does. Without values it is an idle cycle. The queue is empty after every operation, so it
/// never reaches its capacity.
///
/// WriteIntoFullQueue writes into a queue that is at its capacity, so that every value drops the
/// oldest one, logs it and counts it. This is the cost of every value while a source delivers faster
/// than the engine consumes.
///
/// Run with: dotnet run -c Release --project benchmarks/ViciOne.Engine.Benchmarks -- --filter *QueueingRuntimeLink*
/// </summary>
[MemoryDiagnoser]
public class QueueingRuntimeLinkBenchmarks
{
    private ExternalIncomingQueueingRuntimeLink<int, int, int> _fullLink = null!;
    private ExternalIncomingQueueingRuntimeLink<int, int, int> _link = null!;
    private readonly DateTime _time = DateTime.UtcNow;
    // boxed once, because values arrive boxed from the communication or from the source connector
    private readonly object _value = 42;

    private static ExternalIncomingQueueingRuntimeLink<int, int, int> CreateLink()
        => new(new ExternalIncomingLink<int, int, int>(), 0, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

    [GlobalSetup]
    public void Setup()
    {
        _link = CreateLink();
        _fullLink = CreateLink();
        for (int i = 0; i < EngineQueueOptions.DefaultPerLinkQueueCapacity; i++)
            _fullLink.SetData(_value, _time, Validity.Valid);
    }

    [Benchmark]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(100)]
    public int[] WriteAndDrain(int valueCount)
    {
        for (int i = 0; i < valueCount; i++)
            _link.SetData(_value, _time, Validity.Valid);
        return _link.GetAndClearQueuedValues();
    }

    [Benchmark]
    public void WriteIntoFullQueue()
        => _fullLink.SetData(_value, _time, Validity.Valid);
}
