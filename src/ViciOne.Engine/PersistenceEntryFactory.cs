using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class PersistenceEntryFactory : GenericFactory
{
    public PersistenceEntryFactory() : base(GetGenericCreateMethod()) { }

    public IPersistenceEntry Create(IPersistable persistableElement, IReferenceTypeCloneHandler cloneHandler)
        => Create<IPersistenceEntry>(DataTypeCompatibilityValidator.DetermineValueType(persistableElement), [persistableElement, cloneHandler]);

    private static PersistenceEntry<TValue> Create<TValue>(IPersistable<TValue> element, IReferenceTypeCloneHandler cloneHandler)
    {
        TValue? value = cloneHandler.CloneIfNecessary(element.Value);
        return new()
        {
            Time = element.ChangedTime,
            Id = element.Id,
            Value = value,
        };
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(PersistenceEntryFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
