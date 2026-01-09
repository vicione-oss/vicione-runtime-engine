using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class MaxPooling<TValue> : IAggregatingPooling<TValue> where TValue : IComparable
{
    public string Description => "Uses the greatest value of all input values as result.";
    public string Name => "Max";

    public TValue? Aggregate(IEnumerable<TValue?> values)
        => values.Max();
}
