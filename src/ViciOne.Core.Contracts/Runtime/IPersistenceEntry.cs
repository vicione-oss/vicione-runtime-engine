using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IPersistenceEntry : IIdProvider
{
    DateTime Time { get; }
    object? Value { get; }
}
