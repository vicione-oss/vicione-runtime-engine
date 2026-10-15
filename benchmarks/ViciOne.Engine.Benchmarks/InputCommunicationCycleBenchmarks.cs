// BenchmarkDotNet requires public benchmark classes and payload types.
// Disable CA1515 (prefer internal in application projects) for this file.
#pragma warning disable CA1515

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Runtime;

namespace ViciOne.Engine.Benchmarks;

/// <summary>
/// Measures the input communication of one engine cycle: the entries received from the
/// communication manager are taken over from the receive buffer, assigned to their external incoming
/// links and written to them. The links are the ones the engine creates for inputs with the default
/// pooling strategies.
///
/// If there are more entries than links, a link receives several entries per cycle, which makes the
/// processor sort the entries by time.
///
/// Every operation gives the entries a newer time, as a real cycle would receive newer values. With
/// the same time the processor would skip all entries after the first operation, because a link
/// ignores entries that are not newer than its current value.
///
/// The incoming input link groups are replaced, because nothing pools in this benchmark and a
/// queueing link group would overflow otherwise.
///
/// Run with: dotnet run -c Release --project benchmarks/ViciOne.Engine.Benchmarks -- --filter *InputCommunicationCycle*
/// </summary>
[MemoryDiagnoser]
public class InputCommunicationCycleBenchmarks : IDisposable
{
    private StubRuntimeFunctionBlock _block = null!;
    private readonly StubCommunicationManager _communicationManager = new();
    private bool _disposed;
    private LinkChangedEntry<int>[] _entries = null!;
    private LinkCache _linkCache = null!;
    private CommunicationProcessor _processor = null!;
    // the same entries as _entries, but typed as the communication manager hands them over
    private ILinkChangedEntry[] _receivedEntries = null!;
    private DateTime _time = DateTime.UtcNow;

    /// <summary>The number of entries received per cycle.</summary>
    [Params(0, 10, 100, 1000)]
    public int EntryCount { get; set; }

    /// <summary>The number of external incoming links, each connected to an input of its own.</summary>
    [Params(10, 100)]
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
            _processor?.Dispose();
            _linkCache?.Dispose();
            _block?.Dispose();
        }

        _disposed = true;
    }

    [Benchmark]
    public void ReceiveAndProcess()
    {
        _time = _time.AddSeconds(1);
        for (int i = 0; i < _entries.Length; i++)
            _entries[i].Time = _time.AddTicks(i);

        // in an idle cycle nothing is received at all
        if (_receivedEntries.Length > 0)
            _communicationManager.RaiseChangedEntriesReceived(_receivedEntries);
        _processor.ProcessInputCommunication();
    }

    [GlobalSetup]
    public void Setup()
    {
        var inputs = new IRuntimeConnector[LinkCount];
        var links = new ExternalIncomingLink<int, int, int>[LinkCount];
        for (int i = 0; i < LinkCount; i++)
        {
            var input = new ConnectorInput<int> { Id = Guid.NewGuid() };
            inputs[i] = new RuntimeConnector<int>(input, true);
            links[i] = new ExternalIncomingLink<int, int, int> { Id = Guid.NewGuid(), Destination = input };
        }

        _block = new StubRuntimeFunctionBlock(inputs);
        var validator = new DefaultPoolingValidator();
        _linkCache = new LinkCache([_block], links, () => validator,
            EngineQueueOptions.DefaultPerLinkQueueCapacity, EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance,
            createIncomingInputLinkGroup: (connector, connectedLinks, _) => new StubIncomingInputLinkGroup(connector, connectedLinks));
        _processor = new CommunicationProcessor(_linkCache, new DefaultUniqueNameProvider(new Dictionary<Guid, string>()), _communicationManager,
            EngineQueueOptions.DefaultCommunicationReceiveBufferCapacity, NullLogger.Instance);

        _entries = new LinkChangedEntry<int>[EntryCount];
        _receivedEntries = new ILinkChangedEntry[EntryCount];
        for (int i = 0; i < EntryCount; i++)
            _receivedEntries[i] = _entries[i] = new LinkChangedEntry<int> { Id = links[i % LinkCount].Id, Validity = Validity.Valid, Value = i };
    }
}
