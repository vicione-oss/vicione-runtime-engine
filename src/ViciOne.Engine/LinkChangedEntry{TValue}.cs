using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class LinkChangedEntry<TValue> : ILinkChangedEntry<TValue>
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public Validity Validity { get; set; }
    public TValue? Value { get; set; }
    object? ILinkChangedEntry.Value => Value;
}
