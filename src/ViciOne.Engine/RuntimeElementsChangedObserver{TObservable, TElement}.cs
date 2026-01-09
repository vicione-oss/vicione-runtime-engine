using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class RuntimeElementsChangedObserver<TObservable, TElement> : IDisposable
    where TObservable : IChangingElementsProvider<TElement>, IEnumerable<TElement>
    where TElement : notnull
{
    private readonly List<TObservable> _attachedObservables = [];
    private readonly Dictionary<IRuntimeFunctionBlock, List<TElement>> _changedElementsPerBlock = [];
    private bool _disposed;
    private readonly Dictionary<TElement, IRuntimeFunctionBlock> _elementsBlocks = [];

    public RuntimeElementsChangedObserver(IEnumerable<(IRuntimeFunctionBlock, TObservable)> runtimeFunctionBlocks)
    {
        foreach ((IRuntimeFunctionBlock block, TObservable observable) in runtimeFunctionBlocks)
        {
            foreach (TElement element in observable)
                _elementsBlocks[element] = block;

            _changedElementsPerBlock[block] = [];
            AttachToElementChangedEvent(observable);
        }
    }

    private void AttachToElementChangedEvent(TObservable observable)
    {
        observable.ElementChanged += ElementChanged;
        _attachedObservables.Add(observable);
    }

    private void DetachFromElementChangedEvent(TObservable observable)
        => observable.ElementChanged -= ElementChanged;

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            foreach (TObservable observable in _attachedObservables)
                DetachFromElementChangedEvent(observable);
            _attachedObservables.Clear();
            _elementsBlocks.Clear();
            foreach (List<TElement> list in _changedElementsPerBlock.Values)
                list.Clear();
            _changedElementsPerBlock.Clear();
        }

        _disposed = true;
    }

    public IDictionary<IRuntimeFunctionBlock, IEnumerable<TElement>> GetAndResetChanged()
        => _changedElementsPerBlock.Keys
            .Select(x => (Block: x, ChangedElements: GetAndResetChanged(x)))
            .ToDictionary(
                x => x.Block,
                x => x.ChangedElements);

    public IEnumerable<TElement> GetAndResetChanged(IRuntimeFunctionBlock block)
    {
        if (_changedElementsPerBlock.TryGetValue(block, out List<TElement>? elements) == false)
            return [];
        List<TElement> changedElements = elements;
        List<TElement> changedElementsCopy;
        lock (changedElements)
        {
            changedElementsCopy = [.. changedElements];
            changedElements.Clear();
        }
        return changedElementsCopy;
    }

    protected void ElementChanged(TElement element)
    {
        if (_elementsBlocks.TryGetValue(element, out IRuntimeFunctionBlock? block) == false)
            return;

        List<TElement> changedElements = _changedElementsPerBlock[block];
        lock (changedElements)
            if (changedElements.Contains(element) == false)
                changedElements.Add(element);
    }
}
