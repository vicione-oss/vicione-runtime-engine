using System;

namespace ViciOne.Core.Contracts.DataModel;

public interface IPersistable : IIdProvider
{
    DateTime ChangedTime { get; }
}
