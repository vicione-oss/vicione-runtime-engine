using System;
using System.Collections;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime;

public class RuntimePersistentVariables : IRuntimePersistentVariables
{
    private bool _disposed;
    private readonly List<IRuntimePersistentVariable> _variables = [];

    public event Action<IRuntimePersistentVariable>? VariableChanged;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Schnittstellenmethoden müssen von untergeordneten Typen aufgerufen werden können",
        Justification = $"Das Event ist unter dem Namen '{nameof(VariableChanged)}' extern verfügbar.")]
    event Action<IRuntimePersistentVariable>? IChangingElementsProvider<IRuntimePersistentVariable>.ElementChanged
    {
        add => VariableChanged += value;
        remove => VariableChanged -= value;
    }

    public void Add(IRuntimePersistentVariable variable)
    {
        _variables.Add(variable);
        variable.VariableChanged += OnVariableChanged;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            foreach (IRuntimePersistentVariable variable in _variables)
                variable.VariableChanged -= OnVariableChanged;

            _variables.Clear();

            VariableChanged = null;
        }

        _disposed = true;
    }

    public IEnumerator<IRuntimePersistentVariable> GetEnumerator()
        => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    protected virtual void OnVariableChanged(IRuntimePersistentVariable variable)
        => VariableChanged?.Invoke(variable);

    public void ResetChangedFlags()
        => _variables.ForEach(setting => setting.GetAndResetChanged());
}
