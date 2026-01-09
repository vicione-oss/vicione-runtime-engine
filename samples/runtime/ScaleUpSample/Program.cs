using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Engine;
using ViciOne.Serialization.Json;

namespace ScaleUpSample;

internal sealed class Program
{
    private static readonly List<RuntimeEngine> s_engines = [];

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0210:Konvertieren in Anweisungen der obersten Ebene", Justification = "Beispielprojekte einheitlich gestalten")]
    private static void Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        string dataflowPath = args[0];
        string dllDir = args[1];
        int runtimeEngineCount = int.Parse(args[2], CultureInfo.InvariantCulture);
        int timerCount = int.Parse(args[3], CultureInfo.InvariantCulture);

        Assembly[] assemblies = [.. Directory.GetFiles(dllDir, "*.dll", SearchOption.AllDirectories).Select(Assembly.LoadFrom)];
        IDataflow dataflow = Load<Dataflow>(dataflowPath);
        using ILoggerFactory dummyLoggerFactory = new LoggerFactory();
        IUniqueNameProvider uniqueNameProvider = new DefaultUniqueNameProvider(NameGenerator.CreateUniqueNames(dataflow));
        ExecuteEngines(dataflow, assemblies, dummyLoggerFactory, uniqueNameProvider, runtimeEngineCount, timerCount);
    }

    private static void ExecuteEngines(IDataflow dataflow, IEnumerable<Assembly> assemblies, ILoggerFactory loggerFactory, IUniqueNameProvider uniqueNameProvider, int runtimeEngineCount, int timerCount)
    {
        var timers = new MeasuringTimer[timerCount];
        for (int i = 0; i < timerCount; i++)
            timers[i] = new MeasuringTimer(100);

        using var stopWaitHandle = new ManualResetEvent(false);
        for (int i = 0; i < runtimeEngineCount; i++)
        {
            var runtimeEngine = new RuntimeEngine(dataflow, loggerFactory, assemblies, assemblies, uniqueNameProvider);
            s_engines.Add(runtimeEngine);
            MeasuringTimer timer = timers[i % timerCount];
            timer.Engines.Add(runtimeEngine);
        }

        foreach (RuntimeEngine engineToStart in s_engines)
            engineToStart.Start();

        foreach (MeasuringTimer timer in timers)
            timer.Start();

        Console.WriteLine("{0:HH:mm:ss.fff}: started", DateTime.Now);
        MeasuringTimer.EnableOutput = true;

        void ConsoleCancel(object? sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("{0:HH:mm:ss.fff}: stopping", DateTime.Now);

            foreach (MeasuringTimer timer in timers)
                timer.Stop();
            foreach (RuntimeEngine engineToStop in s_engines)
                engineToStop.Stop();
            stopWaitHandle.Set();
            e.Cancel = true;
        }
        Console.CancelKeyPress += ConsoleCancel;

        stopWaitHandle.WaitOne();

        IEnumerable<EngineState> states = s_engines.Select(x => x.State).Distinct();
        while (s_engines.Any(x => x.State != EngineState.Stopped))
            Thread.Sleep(100);

        Console.WriteLine("{0:HH:mm:ss.fff}: avg over all timers: {1}", DateTime.Now, timers.Average(x => x.CalculateAverageElapsed()));

        for (int i = 0; i < timerCount; i++)
            timers[i].Dispose();

        Console.CancelKeyPress -= ConsoleCancel;

        Console.WriteLine("{0:HH:mm:ss.fff}: stopped", DateTime.Now);
    }

    private static T Load<T>(string path)
        => JsonSerialization.Default.LoadFromFile<T>(path);
}
