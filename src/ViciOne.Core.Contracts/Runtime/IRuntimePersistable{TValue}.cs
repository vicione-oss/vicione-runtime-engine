using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimePersistable<TValue> : IPersistable<TValue>, IRuntimePersistable
{

}
