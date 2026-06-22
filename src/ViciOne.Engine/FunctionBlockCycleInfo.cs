using System;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class FunctionBlockCycleInfo(Func<ulong> getCycleNo) : IFunctionBlockCycleInfo
{
    public ulong CycleNo => getCycleNo();
    public bool IsFirstCycle { get; set; } = true;
    public ulong LastProcessedCycleNo { get; set; }
}
