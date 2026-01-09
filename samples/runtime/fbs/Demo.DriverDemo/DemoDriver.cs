using System;
using System.Collections.Generic;
using System.Timers;
using ViciOne.Core.Runtime;

namespace Demo;

public class DemoDriver : RuntimeDriver<DemoDriver>
{
    private bool _disposed;
    private readonly List<string> _machwasList = [];
    private Timer? _timer;

    public DateTime Time { get; private set; }

    public event EventHandler? TimeChanged;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
            Stop();
            TimeChanged = null;
        }
        base.Dispose(disposing);
    }

    public override void InitializeI()
        => base.InitializeI();

    public override void InitializeII()
    {
        base.InitializeII();
        Start();
    }

    public void Machwas(string identifier)
        => _machwasList.Add(identifier);

    private void Start()
    {
        _timer = new Timer { Interval = 1000 };
        _timer.Elapsed += TimerElapsed;
        _timer.Start();
        Logger.DemoDriverTimerStarted();
    }

    private void Stop()
    {
        if (_timer == null)
            return;
        _timer.Elapsed -= TimerElapsed;
        _timer.Stop();
        _timer.Dispose();
        _timer = null;
        Logger.DemoDriverTimerStopped();
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Time = DateTime.Now;
        TimeChanged?.Invoke(this, EventArgs.Empty);
    }
}
