namespace ViciOne.Core.Contracts.DataModel;

public interface ISetting<TValue> : ISetting, IPersistable<TValue>
{
    new ISettingDesign<TValue> Design { get; }
    new IConnector<TValue> Input { get; }
    new IConnector<TValue> Output { get; }
    new TValue? Value { get; set; }
}
