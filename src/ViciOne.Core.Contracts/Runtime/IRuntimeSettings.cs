using System;
using System.Collections.Generic;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeSettings : IEnumerable<IRuntimeSetting>, IDisposable, IChangingElementsProvider<IRuntimeSetting>
{
    event Action? Changed;
    event Action<IRuntimeSetting>? SettingChanged;

    void Add(IRuntimeSetting setting);
    void Process(IFunctionBlockCycleInfo cycleInfo);
    void ResetChangedFlags();
}
