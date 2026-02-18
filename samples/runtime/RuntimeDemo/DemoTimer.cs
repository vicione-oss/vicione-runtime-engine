using System;
using System.Timers;

namespace RuntimeDemo;

internal sealed class DemoTimer : IDisposable
{
    private uint _interval;
    private Timer? _timer;

    public uint Interval
    {
        get => _interval;
        set
        {
            _interval = value;
            _timer?.Interval = _interval;
        }
    }
    public bool IsRunning => _timer is not null && _timer.Enabled;

    public event EventHandler? Tick;

    public void Dispose()
    {
        Stop();
        Tick = null;
    }

    public void Start()
    {
        if (_timer is not null)
            return;

        _timer = new Timer
        {
            Interval = _interval,
            AutoReset = false
        };
        _timer.Elapsed += TimerElapsed;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer is null)
            return;

        _timer.Elapsed -= TimerElapsed;
        _timer.Dispose();
        _timer = null;
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Tick?.Invoke(this, EventArgs.Empty);
        // HACK: einfacher Weg, um sicherzustellen, dass nicht mehrere Engine-Zyklen parallel laufen (Engine ist nicht Eintrittsinvariant)
        _timer?.Start();
    }
}
