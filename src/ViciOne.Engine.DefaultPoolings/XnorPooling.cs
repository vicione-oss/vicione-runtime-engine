using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class XnorPooling : IAggregatingPooling<bool>
{
    public string Description => "Uses logical XNOR to calculate result.";
    public string Name => "XNOR";

    public bool Aggregate(IEnumerable<bool> values)
        => values.Aggregate((x, y) => x ^ y) == false;
}
