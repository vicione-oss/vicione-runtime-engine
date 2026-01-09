using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class NorPooling : IAggregatingPooling<bool>
{
    public string Description => "Uses logical NOR to calculate result.";
    public string Name => "NOR";

    public bool Aggregate(IEnumerable<bool> values)
        => values.Any(x => x) == false;
}
