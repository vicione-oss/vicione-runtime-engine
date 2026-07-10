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
    // Lazily initialized: null for flat/simple object graphs that contain no
    // shared or circular references, saving one Dictionary allocation per call.
    private Dictionary<object, object>? _map;

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
}
