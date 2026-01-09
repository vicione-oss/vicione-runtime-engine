using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime.Stubs;

internal sealed class EmptyRuntimeConnector<TValue> : IRuntimeConnector<TValue>
{
    public bool Changed => default;
    public DateTime ChangedTime => default;
    public IConnector<TValue> Connector => Throw<IConnector<TValue>>();
    IConnector IRuntimeConnector.Connector => Connector;
    public bool Connected => default;
    public Validity Validity => default;
    public TValue? Value => default;
    object? IRuntimeConnector.Value => default;

    public bool GetAndResetChanged()
        => Throw<bool>();

    public void MarkAsChanged()
        => Throw<object>();

    public bool SetValidityIfNotEqual(Validity validity)
        => Throw<bool>();

    public bool SetValueIfNotEqual(TValue? value)
        => Throw<bool>();

    private static T Throw<T>()
        => throw new InvalidOperationException("This runtime connector is only for design purposes and not intended to be used at runtime.");
}
