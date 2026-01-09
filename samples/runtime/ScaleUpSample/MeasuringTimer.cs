using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using ViciOne.Core.Contracts.Runtime;

namespace ScaleUpSample;

internal sealed class MeasuringTimer(uint interval) : IDisposable
{
    private long _cycleCount;
    private long _elapsedSum;
    private readonly uint _interval = interval;
    private bool _isProcessing;
    private Timer? _timer;

    public static bool EnableOutput { get; set; }
    public List<IRuntimeEngine> Engines { get; } = [];

    public void Dispose()
    {
        Stop();
        Engines.Clear();
    }

    public void Start()
    {
        if (_timer != null)
            return;

        _timer = new Timer
        {
            Interval = _interval,
            AutoReset = true
        };
        _timer.Elapsed += TimerElapsed;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer == null)
            return;

        _timer.Elapsed -= TimerElapsed;
        _timer.Dispose();
        _timer = null;
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_isProcessing)
            return;

        _isProcessing = true;
        var watch = Stopwatch.StartNew();
        foreach (IRuntimeEngine engineToProcess in Engines)
            engineToProcess.ProcessCycle();
        watch.Stop();
        _elapsedSum += watch.ElapsedMilliseconds;
        _cycleCount++;
        if (EnableOutput && watch.ElapsedMilliseconds > _interval)
            Console.WriteLine("{0:HH:mm:ss.fff}: {1} > {2}", DateTime.Now, watch.ElapsedMilliseconds, _interval);
        _isProcessing = false;
    }

    public long CalculateAverageElapsed()
        => _cycleCount == 0 ? 0 : _elapsedSum / _cycleCount;
}
