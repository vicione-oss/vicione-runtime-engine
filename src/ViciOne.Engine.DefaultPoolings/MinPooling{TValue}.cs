using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class MinPooling<TValue> : IAggregatingPooling<TValue> where TValue : IComparable
{
    public string Description => "Uses the smallest value of all input values as result.";
    public string Name => "Min";

    public TValue? Aggregate(IEnumerable<TValue?> values)
        => values.Min();
}
