using BenchmarkDotNet.Running;
using ViciOne.Engine.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(CommunicationReceivedEntriesObserverBenchmarks).Assembly).Run(args);
