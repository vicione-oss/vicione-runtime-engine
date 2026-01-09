using System;
using ViciOne.Core.Contracts.Runtime;

namespace RuntimeDemo;

internal sealed class DemoPersistenceEntry : IPersistenceEntry
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public object? Value { get; set; }
}
