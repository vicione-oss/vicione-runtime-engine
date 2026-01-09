using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class AndPooling : IAggregatingPooling<bool>
{
    public string Description => "Uses logical AND to calculate result.";
    public string Name => "AND";

    public bool Aggregate(IEnumerable<bool> values)
        => values.All(x => x);
}
