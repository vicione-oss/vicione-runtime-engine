using System;

namespace ViciOne.Core.Contracts.Runtime;

public interface IPersistenceEntry
{
    Guid Id { get; }
    DateTime Time { get; }
    object? Value { get; }
}
