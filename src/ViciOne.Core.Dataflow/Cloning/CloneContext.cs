using System;
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
    private List<PendingInsertion?>? _insertions;
    private HashSet<object>? _awaitingInsertion;
    private HashSet<object>? _awaitingPopulation;
    private HashSet<object>? _buildingImmutable;
    private int _depth;
    private int _deferrals;

    private readonly record struct PendingWork(
        object Source,
        object Destination,
        ClonePopulateDelegate Populate);

    private sealed record PendingInsertion(
        object Destination,
        List<object?> Dependencies,
        Action Insert);

    /// <summary>
    /// <see langword="true"/> once any population has been deferred. From that moment on a clone
    /// handed out by this context may still be an empty shell, so collections that hash or order
    /// by element state must not insert it — they queue the entry with
    /// <see cref="QueueInsertion"/> instead and fill themselves once the graph is complete.
    /// </summary>
    internal bool HasDeferredWork => _deferrals > 0;

    /// <summary>
    /// How often population has been deferred so far. A caller that has to know whether anything
    /// below <em>its own</em> subtree was deferred captures this before descending and compares
    /// afterwards, rather than asking <see cref="HasDeferredWork"/> about the whole context.
    /// </summary>
    internal int DeferralCount => _deferrals;

    /// <summary>
    /// <see langword="true"/> while <paramref name="clone"/> is still an empty shell: its
    /// population is queued, or it is a collection waiting to be filled.
    /// </summary>
    internal bool IsIncomplete(object clone)
        => (_awaitingPopulation?.Contains(clone) ?? false) ||
           (_awaitingInsertion?.Contains(clone) ?? false);

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
            _awaitingPopulation ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

            _pending.Push(new PendingWork(source, destination, populate));
            _awaitingPopulation.Add(destination);
            _deferrals++;
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

    /// <summary>
    /// Marks <paramref name="source"/> as being rebuilt into an immutable collection. Such a
    /// collection cannot be registered before its contents exist, so a reference cycle back into
    /// it would restart the build forever.
    /// </summary>
    /// <returns><see langword="false"/> when a build for the same instance is already running.</returns>
    internal bool BeginImmutableBuild(object source)
    {
        _buildingImmutable ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        return _buildingImmutable.Add(source);
    }

    /// <summary>Releases the guard taken by <see cref="BeginImmutableBuild"/>.</summary>
    internal void EndImmutableBuild(object source) => _buildingImmutable!.Remove(source);

    /// <summary>
    /// <see langword="true"/> when immutable builds are already nested as deeply as the recursion
    /// guard allows. Such a build cannot be deferred — the collection has to exist complete — so
    /// rejecting it is the only bound left. The set holds exactly the builds currently on the
    /// stack, so its size is the nesting depth.
    /// </summary>
    internal bool IsImmutableBuildDepthExhausted
        => (_buildingImmutable?.Count ?? 0) >= MaxRecursiveDepth;

    /// <summary>
    /// Queues the entries of a collection that hashes or orders by element state. The insertion
    /// runs once every clone it depends on is fully populated.
    /// </summary>
    /// <param name="destination">The collection the entries belong to.</param>
    /// <param name="dependencies">The cloned entries whose state decides where they land — keys,
    /// not values — used to detect dependencies on other queued collections.</param>
    /// <param name="insert">Fills <paramref name="destination"/>.</param>
    internal void QueueInsertion(object destination, List<object?> dependencies, Action insert)
    {
        _insertions ??= [];
        _awaitingInsertion ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

        _insertions.Add(new PendingInsertion(destination, dependencies, insert));
        _awaitingInsertion.Add(destination);
    }

    /// <summary>
    /// Completes population work deferred by the recursion guard, then fills the collections that
    /// had to wait for it. Population runs first so that every clone is fully populated before it
    /// is hashed or ordered.
    /// </summary>
    internal void DrainPendingWork()
    {
        while (_pending is { Count: > 0 })
        {
            PendingWork work = _pending.Pop();

            // Clear first: the work may defer again and re-register the same destination.
            _awaitingPopulation!.Remove(work.Destination);
            PopulateOrDefer(work.Source, work.Destination, work.Populate);
        }

        DrainPendingInsertions();
    }

    private void DrainPendingInsertions()
    {
        if (_insertions is null)
            return;

        int remaining = _insertions.Count;
        while (remaining > 0)
        {
            int inserted = 0;
            for (int i = 0; i < _insertions.Count; i++)
            {
                PendingInsertion? insertion = _insertions[i];
                if (insertion is null || !IsReady(insertion))
                    continue;

                _insertions[i] = null;
                insertion.Insert();
                _awaitingInsertion!.Remove(insertion.Destination);
                remaining--;
                inserted++;
            }

            if (inserted == 0)
            {
                // Every remaining collection waits for another one that waits for it in turn.
                throw new NotSupportedException(
                    "A cycle between collections that hash or order by element state cannot be cloned.");
            }
        }
    }

    /// <summary>
    /// A queued insertion may only run once none of the entries it hashes or orders by is a
    /// collection that is itself still waiting to be filled — an unfilled collection would hash or
    /// compare as empty. A collection that contains itself is exempt: it is already the instance
    /// being filled.
    /// </summary>
    private bool IsReady(PendingInsertion insertion)
    {
        foreach (object? dependency in insertion.Dependencies)
        {
            if (dependency is not null &&
                !ReferenceEquals(dependency, insertion.Destination) &&
                _awaitingInsertion!.Contains(dependency))
            {
                return false;
            }
        }

        return true;
    }
}
