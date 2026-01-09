using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime;

public abstract class RuntimePersistableElement<TPersistable, TValue> : IRuntimePersistableElement where
    TPersistable : class, IPersistable<TValue>
{
    private int _changed;

    public bool Changed => _changed == 1;
    public DateTime ChangedTime { get; protected set; }
    protected TPersistable Element { get; }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Schnittstellenmethoden müssen von untergeordneten Typen aufgerufen werden können", Justification = "Die Id kann über die extern sichtbare Eigenschaft 'Element' abgerufen werden.")]
    Guid IIdProvider.Id => Element.Id;
    public TValue? Value { get; protected set; }

    protected RuntimePersistableElement(TPersistable element)
    {
        Element = element;
        ChangedTime = Element.ChangedTime;
        Value = Element.Value;
    }

    public virtual void ApplyLoadedValue(object? value, DateTime changedtime)
    {
        Value = (TValue?)value;
        ChangedTime = changedtime;
        MarkAsChanged();
    }

    public virtual bool GetAndResetChanged()
        => Interlocked.Exchange(ref _changed, 0) == 1;

    public virtual void MarkAsChanged()
    {
        Interlocked.Exchange(ref _changed, 1);
        OnElementChanged();
    }

    protected abstract void OnElementChanged();

    public virtual bool SetValueIfNotEqual(TValue? value)
    {
        if (EqualityComparer<TValue?>.Default.Equals(Value, value))
            return false;

        Value = value;
        ChangedTime = DateTime.UtcNow;
        MarkAsChanged();
        return true;
    }
}
