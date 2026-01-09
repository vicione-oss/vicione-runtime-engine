using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeLink
{
    bool Changed { get; }
    DateTime ChangedTime { get; }
    ILink Link { get; }
    Validity Validity { get; }
    object? Value { get; }

    event EventHandler<EventArgs>? LinkChanged;

    bool GetAndResetChanged();
    void SetData(object? value, DateTime changedTime, Validity validity);
}
