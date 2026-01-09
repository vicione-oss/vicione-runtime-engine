using System;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class PersistenceEntry<TValue> : IPersistenceEntry<TValue>
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public TValue? Value { get; set; }
    object? IPersistenceEntry.Value => Value;
}
