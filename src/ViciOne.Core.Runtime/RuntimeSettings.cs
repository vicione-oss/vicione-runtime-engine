using System;
using System.Collections;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime;

public class RuntimeSettings : IRuntimeSettings
{
    private bool _disposed;
    private readonly List<IRuntimeSetting> _settings = [];
    private bool _settingsChanged;

    public event Action? Changed;
    public event Action<IRuntimeSetting>? SettingChanged;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Schnittstellenmethoden müssen von untergeordneten Typen aufgerufen werden können",
        Justification = $"Das Event ist unter dem Namen '{nameof(SettingChanged)}' extern verfügbar.")]
    event Action<IRuntimeSetting>? IChangingElementsProvider<IRuntimeSetting>.ElementChanged
    {
        add => SettingChanged += value;
        remove => SettingChanged -= value;
    }

    public void Add(IRuntimeSetting setting)
    {
        _settings.Add(setting);
        setting.SettingChanged += OnSettingChangedInternal;
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
            foreach (IRuntimeSetting setting in _settings)
                setting.SettingChanged -= OnSettingChangedInternal;

            _settings.Clear();

            Changed = null;
            SettingChanged = null;
        }

        _disposed = true;
    }

    public IEnumerator<IRuntimeSetting> GetEnumerator()
        => _settings.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    protected virtual void OnChanged()
        => Changed?.Invoke();

    protected virtual void OnProcess(IFunctionBlockCycleInfo cycleInfo)
        => _settings.ForEach(setting => setting.Process(cycleInfo));

    protected virtual void OnSettingChanged(IRuntimeSetting setting)
        => SettingChanged?.Invoke(setting);

    private void OnSettingChangedInternal(IRuntimeSetting setting)
    {
        _settingsChanged = true;
        OnSettingChanged(setting);
    }

    public void Process(IFunctionBlockCycleInfo cycleInfo)
    {
        OnProcess(cycleInfo);
        if (_settingsChanged)
        {
            OnChanged();
            _settingsChanged = false;
        }
    }

    public void ResetChangedFlags()
        => _settings.ForEach(setting => setting.GetAndResetChanged());
}
