using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeConnector<TValue> : IRuntimeConnector
{
    new IConnector<TValue> Connector { get; }
    new TValue? Value { get; }

    bool SetValueIfNotEqual(TValue? value);
}
