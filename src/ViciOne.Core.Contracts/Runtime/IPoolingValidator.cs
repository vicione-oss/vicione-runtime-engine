using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IPoolingValidator
{
    bool IsLinkValid<TValue>(IRuntimeLink<TValue> link);
    Validity GetResultValidity<TValue>(IRuntimeLink<TValue>[] links);
    IEnumerable<IRuntimeLink<TValue>> SelectLinks<TValue>(IEnumerable<IRuntimeLink<TValue>> links);
}
