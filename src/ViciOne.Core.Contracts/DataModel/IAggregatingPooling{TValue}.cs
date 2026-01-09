using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IAggregatingPooling<TValue> : IAggregatingPooling
{
    TValue? Aggregate(IEnumerable<TValue?> values);
}
