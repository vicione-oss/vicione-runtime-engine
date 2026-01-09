using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeConnector
{
    bool Changed { get; }
    DateTime ChangedTime { get; }
    IConnector Connector { get; }
    bool Connected { get; }
    Validity Validity { get; }
    object? Value { get; }

    bool GetAndResetChanged();
    void MarkAsChanged();
    bool SetValidityIfNotEqual(Validity validity);
}
