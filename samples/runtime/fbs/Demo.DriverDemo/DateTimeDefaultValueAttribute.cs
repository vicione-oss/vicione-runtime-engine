using System;
using ViciOne.Core.Runtime.Attributes;

namespace Demo;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments",
    Justification = "The values of the arguments are made available through the created DateTime instance via the base class's Value property.")]
public sealed class DateTimeDefaultValueAttribute(int year, int month, int day, int hour, int minute, int second) :
    DefaultValueAttribute(new DateTime(year, month, day, hour, minute, second))
{ }
