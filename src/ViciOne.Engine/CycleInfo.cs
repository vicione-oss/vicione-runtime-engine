using System;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class CycleInfo : ICycleInfo
{
    public ulong CycleNo { get; set; }
    public DateTime Time { get; set; }
}
