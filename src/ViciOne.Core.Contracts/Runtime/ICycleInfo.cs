using System;

namespace ViciOne.Core.Contracts.Runtime;

public interface ICycleInfo
{
    ulong CycleNo { get; }
    DateTime Time { get; }
}
