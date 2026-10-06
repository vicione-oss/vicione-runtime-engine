// BenchmarkDotNet requires public benchmark classes and payload types.
// Disable CA1515 (prefer internal in application projects) for this file.
#pragma warning disable CA1515

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine.Benchmarks;

/// <summary>
/// Measures one engine cycle of the buffer between the communication manager and the engine: the
/// entries received since the last cycle are added to the buffer, then the engine takes them over.
///
/// The buffer is drained in every operation, as the engine does in every cycle, so that it never
/// fills up to its capacity. Otherwise the measurement would switch to the overflow path after a
/// few thousand operations, where all received entries are dropped.
///
/// With no entries the operation is an idle cycle, which is by far the most frequent case.
///
/// Run with: dotnet run -c Release --project benchmarks/ViciOne.Engine.Benchmarks -- --filter *CommunicationReceivedEntriesObserver*
/// </summary>
[MemoryDiagnoser]
public class CommunicationReceivedEntriesObserverBenchmarks : IDisposable
{
    private readonly StubCommunicationManager _communicationManager = new();
    private bool _disposed;
    private ILinkChangedEntry[] _entries = null!;
    private CommunicationReceivedEntriesObserver _observer = null!;

    /// <summary>The number of entries received per cycle.</summary>
    [Params(0, 1, 10, 100, 1000)]
    public int EntryCount { get; set; }

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
            _observer?.Dispose();

        _disposed = true;
    }

    // returned as object, because BenchmarkDotNet would otherwise demand to enumerate the entries,
    // which is the work of the engine and not of the buffer
    [Benchmark]
    public object ReceiveAndDrain()
    {
        // in an idle cycle nothing is received at all
        if (_entries.Length > 0)
            _communicationManager.RaiseChangedEntriesReceived(_entries);
        return _observer.GetAndResetReceivedEntries();
    }

    [GlobalSetup]
    public void Setup()
    {
        _observer = new(_communicationManager, EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, NullLogger.Instance);

        DateTime now = DateTime.UtcNow;
        _entries = new ILinkChangedEntry[EntryCount];
        for (int i = 0; i < EntryCount; i++)
            _entries[i] = new LinkChangedEntry<int> { Id = Guid.NewGuid(), Time = now.AddTicks(i), Validity = Validity.Valid, Value = i };
    }
}
