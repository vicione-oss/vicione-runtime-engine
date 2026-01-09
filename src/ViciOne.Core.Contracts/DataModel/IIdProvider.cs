using System;

namespace ViciOne.Core.Contracts.DataModel;

public interface IIdProvider
{
    Guid Id { get; }
}
