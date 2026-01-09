using System;
using System.Collections.Generic;

internal sealed class DisposableList<T> : List<T>, IDisposable where T : IDisposable
{
    public void Dispose()
        => ForEach(x => x.Dispose());
}
