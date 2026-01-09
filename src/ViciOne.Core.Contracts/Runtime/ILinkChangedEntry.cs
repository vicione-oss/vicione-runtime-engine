using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface ILinkChangedEntry : IIdProvider
{
    DateTime Time { get; }
    Validity Validity { get; }
    object? Value { get; }
}
