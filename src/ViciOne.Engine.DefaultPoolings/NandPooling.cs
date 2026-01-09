using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class NandPooling : IAggregatingPooling<bool>
{
    public string Description => "Uses logical NAND to calculate result.";
    public string Name => "NAND";

    public bool Aggregate(IEnumerable<bool> values)
        => values.All(x => x) == false;
}
