using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class SumPooling :
    IAggregatingPooling<double>,
    IAggregatingPooling<long>
{
    public string Description => "Calculates the sum of all current input values.";
    public string Name => "Sum";

    public double Aggregate(IEnumerable<double> values)
        => values.Sum();

    public long Aggregate(IEnumerable<long> values)
        => values.Sum();
}
