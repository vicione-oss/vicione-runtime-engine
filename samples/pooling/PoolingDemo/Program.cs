using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Engine.DefaultPoolings;

namespace PoolingDemo;

internal sealed class Program
{
    public static void Main()
    {
        // laden des Pooling-Assemblies erzwingen
        var _ = new SumPooling();
        DesignerDemo();
    }

    private static void DesignerDemo()
    {
        // alle verfügbaren Pooling-Modes für den entsprechenden Datentyp auflisten
        // (für die Demo hier wird vorausgesetzt, dass die Assemblies, die die Poolings enthalten, bereits geladen sind)

        var poolingFactory = new PoolingFactory(AppDomain.CurrentDomain.GetAssemblies());

        IEnumerable<Type> a = poolingFactory.GetAvailablePoolingTypes<bool>();
        IEnumerable<Type> b = poolingFactory.GetAvailablePoolingTypes<double>();
        IEnumerable<Type> c = poolingFactory.GetAvailablePoolingTypes<ComparableDings>();
        IEnumerable<Type> d = poolingFactory.GetAvailablePoolingTypes<UncomparableDings>();

        var aa = a.Select(PoolingFactory.GetNewPooling<bool>).ToList();
        var bb = b.Select(PoolingFactory.GetNewPooling<double>).ToList();
        var cc = c.Select(PoolingFactory.GetNewPooling<ComparableDings>).ToList();
        var dd = d.Select(PoolingFactory.GetNewPooling<UncomparableDings>).ToList();

        aa.Sort(PoolingComparer<bool>.Default);
        bb.Sort(PoolingComparer<double>.Default);
        cc.Sort(PoolingComparer<ComparableDings>.Default);
        dd.Sort(PoolingComparer<UncomparableDings>.Default);

        // Standard-Pooling herausfinden
        IAggregatingPooling<bool>? defaultPoolingA = poolingFactory.GetNewDefaultPooling<bool>();
        IAggregatingPooling<double>? defaultPoolingB = poolingFactory.GetNewDefaultPooling<double>();
        IAggregatingPooling<ComparableDings>? defaultPoolingC = poolingFactory.GetNewDefaultPooling<ComparableDings>();
    }

    [SuppressMessage("Performance", "CA1812:Nicht instanziierte interne Klassen vermeiden.", Justification = "Die Klasse wird durch die Benutzung der PoolingFactory verwendet.")]
    private sealed class ComparableDings : IComparable
    {
        public int Eins { get; set; }
        public bool Zwei { get; set; }

        public int CompareTo(object? obj) => 0;
    }

    [SuppressMessage("Performance", "CA1812:Nicht instanziierte interne Klassen vermeiden.", Justification = "Die Klasse wird durch die Benutzung der PoolingFactory verwendet.")]
    private sealed class UncomparableDings
    {
        public int Eins { get; set; }
        public bool Zwei { get; set; }
    }
}
