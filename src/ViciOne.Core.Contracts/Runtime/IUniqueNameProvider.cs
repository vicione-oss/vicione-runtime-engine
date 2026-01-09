using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IUniqueNameProvider
{
    string GetUniqueName(IIdProvider idProvider);
}
