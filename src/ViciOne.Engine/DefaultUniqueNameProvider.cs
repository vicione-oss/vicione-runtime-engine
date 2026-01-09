using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public class DefaultUniqueNameProvider(IDictionary<Guid, string> identifierNames) : IUniqueNameProvider
{
    public static readonly string UndefinedName = "<undefined>";

    private readonly IDictionary<Guid, string> _identifierNames = identifierNames;

    public virtual string GetUniqueName(IIdProvider idProvider)
        => _identifierNames.TryGetValue(idProvider.Id, out string? uniqueName) ? uniqueName : UndefinedName;
}
