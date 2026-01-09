using System;

namespace ViciOne.Core.Contracts.DataModel;

public interface ISetting : IPersistable
{
    new DateTime ChangedTime { get; set; }
    ISettingDesign Design { get; }
    IConnector Input { get; }
    IConnector Output { get; }
    object? Value { get; set; }
}
