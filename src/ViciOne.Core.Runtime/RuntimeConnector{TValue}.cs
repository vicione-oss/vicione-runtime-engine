using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime;

[DebuggerDisplay("{Connector.Design.Name,nq}")]
public class RuntimeConnector<TValue> : IRuntimeConnector<TValue>
{
    private int _changed;

    public bool Changed => _changed == 1;
    public DateTime ChangedTime { get; protected set; }
    public IConnector<TValue> Connector { get; }
    IConnector IRuntimeConnector.Connector => Connector;
    public bool Connected { get; }
    public Validity Validity { get; protected set; } = RuntimeConnectorDefaults.DefaultValidity;
    public TValue? Value { get; protected set; }
    object? IRuntimeConnector.Value => Value;

    public RuntimeConnector(IConnector<TValue> connector, bool connected)
    {
        Connector = connector;
        Connected = connected;
        Value = ValidateValueBase(Connector.Value);
    }

    public virtual bool GetAndResetChanged()
        => Interlocked.Exchange(ref _changed, 0) == 1;

    public virtual void MarkAsChanged()
    {
        ChangedTime = DateTime.UtcNow;
        Interlocked.Exchange(ref _changed, 1);
    }

    public virtual bool SetValueIfNotEqual(TValue? value)
    {
        value = ValidateValue(value);

        if (Connector.MarkAsChangedOnlyIfNotEqual && EqualityComparer<TValue?>.Default.Equals(Value, value))
            return false;

        Value = value;
        MarkAsChanged();
        return true;
    }

    public virtual bool SetValidityIfNotEqual(Validity validity)
    {
        if (Validity == validity)
            return false;

        Validity = validity;
        MarkAsChanged();
        return true;
    }

    protected virtual TValue? ValidateValue(TValue? value)
        => ValidateValueBase(value);

    protected TValue? ValidateValueBase(TValue? value)
    {
        // zweistufige Validierung
        foreach (IValidator<TValue> validator in Connector.Design.Validators)
            value = validator.Validate(value, false);
        foreach (IValidator<TValue> validator in Connector.Validators)
            value = validator.Validate(value, false);
        return value;
    }
}
