using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Stubs;

internal sealed class EmptyAggregatingPooling<TValue> : IAggregatingPooling<TValue>
{
    public string Description => string.Empty;
    public string Name => string.Empty;

    TValue? IAggregatingPooling<TValue>.Aggregate(IEnumerable<TValue?> values)
        => throw new InvalidOperationException("This pooling is only for design purposes and not intended to be used at runtime.");
}
