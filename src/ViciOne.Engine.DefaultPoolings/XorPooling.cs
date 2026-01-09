using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class XorPooling : IAggregatingPooling<bool>
{
    public string Description => "Uses logical XOR to calculate result.";
    public string Name => "XOR";

    public bool Aggregate(IEnumerable<bool> values)
        => values.Aggregate((x, y) => x ^ y);
}
