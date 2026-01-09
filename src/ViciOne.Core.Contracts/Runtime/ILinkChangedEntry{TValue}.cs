namespace ViciOne.Core.Contracts.Runtime;

public interface ILinkChangedEntry<TValue> : ILinkChangedEntry
{
    new TValue? Value { get; }
}
