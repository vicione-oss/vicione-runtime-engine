using System;
using System.ComponentModel;
using System.Globalization;

namespace ViciOne.Core.Dataflow.DataModel.DefaultTypeConverters;

public class DateAndTimeToDateTimeConverter : DateTimeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(DateOnly) ||
           sourceType == typeof(TimeOnly) ||
           base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value switch
        {
            DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
            TimeOnly timeOnly => DateTime.MinValue.Add(timeOnly.ToTimeSpan()),
            _ => base.ConvertFrom(context, culture, value),
        };
}
