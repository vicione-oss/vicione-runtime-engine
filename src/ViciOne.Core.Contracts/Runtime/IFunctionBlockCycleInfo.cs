namespace ViciOne.Core.Contracts.Runtime;

public interface IFunctionBlockCycleInfo
{
    ulong CycleNo { get; }
    bool IsFirstCycle { get; }
    ulong LastProcessedCycleNo { get; }
}
