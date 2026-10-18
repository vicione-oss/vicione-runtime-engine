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
/// Measures the aggregating link group of an input, which aggregates the values of all links
/// connected to the input into one value per cycle.
///
/// Every operation is one cycle in which every link receives a value and the group pools them. The
/// group takes over one queued value per link and cycle, or all of them with per source
/// aggregation, so one value per link and cycle is the steady state.
///
/// The aggregation uses MaxPooling, which allocates nothing, so that the measured allocations are
/// those of the group and its links.
/// </summary>
[MemoryDiagnoser]
public class AggregatingIncomingInputLinkGroupBenchmarks : IDisposable
{
    private AggregatingIncomingInputLinkGroup<int> _aggregatingGroup = null!;
    private ExternalIncomingQueueingRuntimeLink<int, int, int>[] _aggregatingLinks = null!;
    private bool _disposed;
    private AggregatingIncomingInputLinkGroup<int> _group = null!;
    private readonly DefaultConnectorValueTransferInterceptor _interceptor = new();
    private ExternalIncomingQueueingRuntimeLink<int, int, int>[] _links = null!;
    private readonly DateTime _time = DateTime.UtcNow;
    private readonly DefaultPoolingValidator _validator = new();
    // boxed once, because values arrive boxed from the communication or from the source connector
    private readonly object _value = 42;

    /// <summary>The number of links connected to the input.</summary>
    [Params(2, 4, 16)]
    public int LinkCount { get; set; }

    private static ExternalIncomingQueueingRuntimeLink<int, int, int>[] CreateLinks(ConnectorInput<int> input, int linkCount)
    {
        var links = new ExternalIncomingQueueingRuntimeLink<int, int, int>[linkCount];
        for (int i = 0; i < linkCount; i++)
            links[i] = new(new ExternalIncomingLink<int, int, int> { Id = Guid.NewGuid(), Destination = input }, 0,
                EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
        return links;
    }

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
            _interceptor.Dispose();

        _disposed = true;
    }

    [Benchmark]
    public void ReceiveAndPool()
    {
        foreach (ExternalIncomingQueueingRuntimeLink<int, int, int> link in _links)
            link.SetData(_value, _time, Validity.Valid);
        _group.Pool(_validator, _interceptor);
    }

    [Benchmark]
    public void ReceiveAndPoolWithPerSourceAggregation()
    {
        foreach (ExternalIncomingQueueingRuntimeLink<int, int, int> link in _aggregatingLinks)
            link.SetData(_value, _time, Validity.Valid);
        _aggregatingGroup.Pool(_validator, _interceptor);
    }

    [GlobalSetup]
    public void Setup()
    {
        var input = new ConnectorInput<int>
        {
            CrossSourceAggregatingPooling = new MaxPooling<int>(),
            CrossSourcePoolingStrategy = PoolingStrategy.Aggregate,
            Id = Guid.NewGuid(),
        };
        _links = CreateLinks(input, LinkCount);
        _group = new(new RuntimeConnector<int>(input, true), _links);

        var aggregatingInput = new ConnectorInput<int>
        {
            CrossSourceAggregatingPooling = new MaxPooling<int>(),
            CrossSourcePoolingStrategy = PoolingStrategy.Aggregate,
            Id = Guid.NewGuid(),
            PerSourceAggregatingPooling = new MaxPooling<int>(),
            PerSourcePoolingStrategy = PoolingStrategy.Aggregate,
        };
        _aggregatingLinks = CreateLinks(aggregatingInput, LinkCount);
        _aggregatingGroup = new(new RuntimeConnector<int>(aggregatingInput, true), _aggregatingLinks);
    }
}
