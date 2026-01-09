using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace RuntimeDemo;

internal sealed class DemoLinkChangedEntry : ILinkChangedEntry
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public Validity Validity { get; set; }
    public object? Value { get; set; }
}
