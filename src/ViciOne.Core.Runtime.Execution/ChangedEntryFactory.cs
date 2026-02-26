using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime.Execution;

internal static class ChangedEntryFactory
{
    public static ChangedEntry Create(ILinkChangedEntry linkChangedEntry)
       => new()
       {
           Time = linkChangedEntry.Time,
           Id = linkChangedEntry.Id,
           Validity = linkChangedEntry.Validity,
           Value = linkChangedEntry.Value,
       };

    public static ChangedEntry Create(IPersistenceEntry persistenceEntry)
       => new()
       {
           Time = persistenceEntry.Time,
           Id = persistenceEntry.Id,
           Validity = Validity.Valid,
           Value = persistenceEntry.Value,
       };
}
