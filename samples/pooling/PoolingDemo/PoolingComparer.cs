using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace PoolingDemo;

internal sealed class PoolingComparer<T> : IComparer<IAggregatingPooling<T>>
{
    public static PoolingComparer<T> Default { get; } = new PoolingComparer<T>();

    private PoolingComparer() { }

    public int Compare(IAggregatingPooling<T>? x, IAggregatingPooling<T>? y)
    {
        if (x is null && y is null)
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
    }
}
