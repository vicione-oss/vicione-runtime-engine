using System;

namespace ViciOne.Core.Contracts.Runtime;

public interface IChangingElementsProvider<TElement>
{
    event Action<TElement>? ElementChanged;
}
