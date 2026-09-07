using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;

namespace ViciOne.Engine;

public class DefaultPoolingValidator : IPoolingValidator
{
    public virtual bool IsLinkValid<TValue>(IRuntimeLink<TValue> link)
        // TODO: later on a concept should be implemented here that decides for how long a link stays valid
        => link.Validity == Validity.Valid;

    public virtual Validity GetResultValidity<TValue>(IRuntimeLink<TValue>[] links)
        => links.Length > 0 ? links.Min(x => x.Validity) : RuntimeConnectorDefaults.DefaultValidity;

    public virtual IEnumerable<IRuntimeLink<TValue>> SelectLinks<TValue>(IEnumerable<IRuntimeLink<TValue>> links)
        => links.Where(x => IsLinkValid(x));
}
