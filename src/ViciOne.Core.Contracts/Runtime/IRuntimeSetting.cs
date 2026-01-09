using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeSetting : IRuntimePersistableElement
{
    IRuntimeConnector Input { get; }
    IRuntimeConnector Output { get; }
    ISetting Setting { get; }

    event Action<IRuntimeSetting>? SettingChanged;

    void Process(IFunctionBlockCycleInfo cycleInfo);
}
