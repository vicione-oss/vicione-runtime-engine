using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeLink<TValue> : IRuntimeLink
{
    new ILink<TValue> Link { get; }
    new TValue? Value { get; }
}
