using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;

namespace ViciOne.Engine.DefaultPoolings;

[DefaultPoolingForType<bool>]
public class OrPooling : IAggregatingPooling<bool>
{
    public string Description => "Uses logical OR to calculate result.";
    public string Name => "OR";

    public bool Aggregate(IEnumerable<bool> values)
        => values.Any(x => x);
}
