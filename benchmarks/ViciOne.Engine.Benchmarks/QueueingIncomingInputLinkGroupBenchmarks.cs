// BenchmarkDotNet requires public benchmark classes and payload types.
// Disable CA1515 (prefer internal in application projects) for this file.
#pragma warning disable CA1515

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Runtime;
using ViciOne.Engine.DefaultPoolings;

namespace ViciOne.Engine.Benchmarks;

/// <summary>
/// Measures the queueing link group of an input, which collects the values of all links connected
/// to the input and transfers one of them per cycle.
///
/// Every operation is one cycle in which one of the links receives a value and the group pools it.
/// The group transfers only one value per cycle, so one value per cycle is the steady state. More
/// values would fill the queue of the group up to its capacity.
///
/// With per source aggregation the group also takes the values queued in every link and aggregates
/// them per link, so its cost grows with the number of links even if only one of them received a
/// value. In that mode the group checks every link for changes to know whether it needs further
/// processing.
///
/// Run with: dotnet run -c Release --project benchmarks/ViciOne.Engine.Benchmarks -- --filter *QueueingIncomingInputLinkGroup*
/// </summary>
[MemoryDiagnoser]
public class QueueingIncomingInputLinkGroupBenchmarks : IDisposable
{
    private QueueingIncomingInputLinkGroup<int> _aggregatingGroup = null!;
    private ExternalIncomingQueueingRuntimeLink<int, int, int>[] _aggregatingLinks = null!;
    private bool _disposed;
    private QueueingIncomingInputLinkGroup<int> _group = null!;
    private readonly DefaultConnectorValueTransferInterceptor _interceptor = new();
    private ExternalIncomingRuntimeLink<int, int, int>[] _links = null!;
    private int _nextLink;
    private readonly DateTime _time = DateTime.UtcNow;
    private readonly DefaultPoolingValidator _validator = new();
    // boxed once, because values arrive boxed from the communication or from the source connector
    private readonly object _value = 42;

    /// <summary>The number of links connected to the input.</summary>
    [Params(1, 10, 50)]
    public int LinkCount { get; set; }

    [GlobalCleanup]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _group?.Dispose();
            _aggregatingGroup?.Dispose();
            _interceptor.Dispose();
        }

        _disposed = true;
    }

    private int GetNextLink()
        => _nextLink = (_nextLink + 1) % LinkCount;

    [Benchmark]
    public bool NeedsFurtherProcessingWithPerSourceAggregation()
        => _aggregatingGroup.NeedsFurtherProcessing;

    [Benchmark]
    public void ReceiveAndPool()
    {
        _links[GetNextLink()].SetData(_value, _time, Validity.Valid);
        _group.Pool(_validator, _interceptor);
    }

    [Benchmark]
    public void ReceiveAndPoolWithPerSourceAggregation()
    {
        _aggregatingLinks[GetNextLink()].SetData(_value, _time, Validity.Valid);
        _aggregatingGroup.Pool(_validator, _interceptor);
    }

    [GlobalSetup]
    public void Setup()
    {
        var input = new ConnectorInput<int> { Id = Guid.NewGuid() };
        _links = new ExternalIncomingRuntimeLink<int, int, int>[LinkCount];
        for (int i = 0; i < LinkCount; i++)
            _links[i] = new(new ExternalIncomingLink<int, int, int> { Id = Guid.NewGuid(), Destination = input }, 0);
        _group = new(new RuntimeConnector<int>(input, true), _links, () => _validator,
            EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);

        var aggregatingInput = new ConnectorInput<int>
        {
            Id = Guid.NewGuid(),
            PerSourceAggregatingPooling = new MaxPooling<int>(),
            PerSourcePoolingStrategy = PoolingStrategy.Aggregate,
        };
        _aggregatingLinks = new ExternalIncomingQueueingRuntimeLink<int, int, int>[LinkCount];
        for (int i = 0; i < LinkCount; i++)
            _aggregatingLinks[i] = new(new ExternalIncomingLink<int, int, int> { Id = Guid.NewGuid(), Destination = aggregatingInput }, 0,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
        _aggregatingGroup = new(new RuntimeConnector<int>(aggregatingInput, true), _aggregatingLinks, () => _validator,
            EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance);
    }
}
