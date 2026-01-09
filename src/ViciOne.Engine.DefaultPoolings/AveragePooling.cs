using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;

namespace ViciOne.Engine.DefaultPoolings;

[DefaultPoolingForType<double>]
[DefaultPoolingForType<long>]
public class AveragePooling :
    IAggregatingPooling<double>,
    IAggregatingPooling<long>
{
    public string Description => "Calculates average value of all current input values.";
    public string Name => "Average";

    public double Aggregate(IEnumerable<double> values)
        => values.Average();

    public long Aggregate(IEnumerable<long> values)
        => (long)Math.Round(values.Average());
}
