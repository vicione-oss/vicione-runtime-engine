using System;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class FunctionBlockCycleInfo(Func<ulong> getCycleNo) : IFunctionBlockCycleInfo
{
    private readonly Func<ulong> _getCycleNo = getCycleNo;

    public ulong CycleNo => _getCycleNo();
    public bool IsFirstCycle { get; set; } = true;
    public ulong LastProcessedCycleNo { get; set; }
}
