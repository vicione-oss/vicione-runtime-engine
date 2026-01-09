using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;

namespace Demo;

[DefaultPoolingForType<Base>]
public class BaseAveragePooling : IAggregatingPooling<Base>
{
    public string Description => "Sample Pooling for aggregating type 'Base'";
    public string Name => "Base Avg Sample Pooling";

    public Base? Aggregate(IEnumerable<Base?> values)
    {
        Base[] notNullValues = [.. values.Where(x => x is not null).Select(x => x!)];
        if (notNullValues.Length == 0)
            return null;
        int avg = (int)Math.Round(notNullValues.Average(x => x.NumericValue), 0);
        return notNullValues.All(x => x is Derived)
            ? new Derived() { NumericValue = avg }
            : new Base() { NumericValue = avg };
    }
}
