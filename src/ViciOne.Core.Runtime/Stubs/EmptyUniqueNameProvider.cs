using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime.Stubs;

internal sealed class EmptyUniqueNameProvider : IUniqueNameProvider
{
    public string GetUniqueName(IIdProvider idProvider)
        => throw new InvalidOperationException("This unique name generator is only for design purposes and not intended to be used at runtime.");
}
