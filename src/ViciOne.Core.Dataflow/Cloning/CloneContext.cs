using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Core.Dataflow.Cloning;

/// <summary>
/// Tracks the original → clone mapping for one top-level <c>DeepClone</c> call.
/// Keyed by reference identity so that shared and circular references are detected
/// correctly. Not thread-safe; a fresh instance is created per <c>DeepClone</c> call.
/// </summary>
internal sealed class CloneContext
{
    // A field clone passes through several framework/expression frames per graph level.
    // Keep a conservative bound, then continue through the explicit work stack.
    private const int MaxRecursiveDepth = 128;

    // Lazily initialized: null for flat/simple object graphs that contain no
    // shared or circular references, saving one Dictionary allocation per call.
    private Dictionary<object, object>? _map;
    private Stack<PendingWork>? _pending;
    private int _depth;

    private readonly record struct PendingWork(
        object Source,
        object Destination,
        ClonePopulateDelegate Populate);

    /// <summary>
    /// Returns <see langword="true"/> and sets <paramref name="clone"/> when
    /// <paramref name="source"/> has already been cloned in this operation.
    /// </summary>
    internal bool TryGetClone(object source, [NotNullWhen(true)] out object? clone)
    {
        if (_map is null)
        {
            clone = null;
            return false;
        }
        return _map.TryGetValue(source, out clone);
    }

    /// <summary>
    /// Registers the mapping from <paramref name="source"/> to its <paramref name="clone"/>
    /// so that subsequent encounters of the same original return the same clone instance.
    /// </summary>
    internal void Register(object source, object clone)
    {
        _map ??= new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        _map.Add(source, clone);
    }

    /// <summary>
    /// Populates a registered clone immediately while recursion remains bounded; otherwise
    /// defers the work until the current recursive branch unwinds.
    /// </summary>
    internal void PopulateOrDefer(
        object source,
        object destination,
        ClonePopulateDelegate populate)
    {
        if (_depth >= MaxRecursiveDepth)
        {
            _pending ??= new Stack<PendingWork>();
            _pending.Push(new PendingWork(source, destination, populate));
            return;
        }

        _depth++;
        try
        {
            populate(source, destination, this);
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>Completes population work deferred by the recursion guard.</summary>
    internal void DrainPendingWork()
    {
        while (_pending is { Count: > 0 })
        {
            PendingWork work = _pending.Pop();
            PopulateOrDefer(work.Source, work.Destination, work.Populate);
        }
    }
}
