using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimePersistable : IPersistable
{
    void ApplyLoadedValue(object? value, DateTime changedtime);
}
