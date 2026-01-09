using System;
using ViciOne.Core.Runtime.Attributes;

namespace Demo;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Zugriffsmethoden für Attributargumente definieren",
    Justification = "Die Werte der Argumente werden durch die erstellte DateTime-Instanz über die Value-Eigenschaft der Basis verfügbar gemacht.")]
public sealed class DateTimeDefaultValueAttribute(int year, int month, int day, int hour, int minute, int second) :
    DefaultValueAttribute(new DateTime(year, month, day, hour, minute, second))
{ }
