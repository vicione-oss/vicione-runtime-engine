using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class LinkChangedEntryFactory : GenericFactory
{
    public LinkChangedEntryFactory() : base(GetGenericCreateMethod()) { }

    public ILinkChangedEntry Create(IRuntimeLink link, IReferenceTypeCloneHandler cloneHandler)
        => Create<ILinkChangedEntry>(DataTypeCompatibilityValidator.DetermineValueType(link), [link, cloneHandler]);

    private static LinkChangedEntry<TValue> Create<TValue>(IRuntimeLink<TValue> link, IReferenceTypeCloneHandler cloneHandler)
    {
        TValue? value = cloneHandler.CloneIfNecessary(link.Value);
        return new()
        {
            Time = link.ChangedTime,
            Id = link.Link.Id,
            Validity = link.Validity,
            Value = value,
        };
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(LinkChangedEntryFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
