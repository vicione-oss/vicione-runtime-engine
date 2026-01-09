using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class RuntimeElementsProvider<TElementProvider, TElement> : IDisposable
    where TElementProvider : IEnumerable<TElement>
{
    private bool _disposed;
    private readonly Dictionary<IRuntimeFunctionBlock, IEnumerable<TElement>> _elementsPerBlock = [];

    public RuntimeElementsProvider(IEnumerable<(IRuntimeFunctionBlock, TElementProvider)> runtimeFunctionBlocks)
    {
        foreach ((IRuntimeFunctionBlock block, TElementProvider blockElements) in runtimeFunctionBlocks)
            _elementsPerBlock[block] = blockElements.ToArray();
    }

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _elementsPerBlock.Clear();

        _disposed = true;
    }

    public IDictionary<IRuntimeFunctionBlock, IEnumerable<TElement>> GetAll()
        => _elementsPerBlock;
}
