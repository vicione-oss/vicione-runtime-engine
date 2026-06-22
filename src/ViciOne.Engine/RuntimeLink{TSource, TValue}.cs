using System;
using System.Threading;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;

namespace ViciOne.Engine;

internal abstract class RuntimeLink<TSource, TValue> : IRuntimeLink<TValue>
{
    private int _changed;

    public bool Changed => _changed == 1;
    public DateTime ChangedTime { get; protected set; }
    public ILink<TValue> Link { get; }
    ILink IRuntimeLink.Link => Link;
    public Validity Validity { get; protected set; } = RuntimeConnectorDefaults.DefaultValidity;
    public TValue? Value { get; protected set; }
    object? IRuntimeLink.Value => Value;

    public event EventHandler<EventArgs>? LinkChanged;

    public RuntimeLink(ILink<TValue> link, object? value, bool changed = false)
    {
        Link = link;
        Value = ConvertValue(value);
        _changed = changed ? 1 : 0;
    }

    protected TValue? ConvertValue(object? value)
        => Link.TypeConversion is null || value is null
            ? (TValue?)value
            : Link.TypeConversion.UseConvertFrom
                ? (TValue?)Link.TypeConversion.TypeConverter.ConvertFrom(value)
                : (TValue?)Link.TypeConversion.TypeConverter.ConvertTo(value, typeof(TValue));

    public virtual bool GetAndResetChanged()
        // TODO: prüfen, ob die Changed-Markierung eines Links überhaupt threadsicher sein muss
        => Interlocked.Exchange(ref _changed, 0) == 1;

    protected virtual void MarkAsChanged()
        => Interlocked.Exchange(ref _changed, 1);

    protected virtual void OnLinkChanged()
        => LinkChanged?.Invoke(this, EventArgs.Empty);

    protected virtual void SetConvertedData(TValue? value, DateTime changedTime, Validity validity)
    {
        Value = value;
        Validity = validity;
        ChangedTime = changedTime;
        MarkAsChanged();
        OnLinkChanged();
    }

    public virtual void SetData(object? value, DateTime changedTime, Validity validity)
        => SetConvertedData(ConvertValue(value), changedTime, validity);
}
