using System;
using System.IO;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Serialization.Json;

public abstract class LoadingAndSaving
{
    protected PoolingFactory PoolingFactory { get; }
    protected string WorkingDir { get; }

    protected LoadingAndSaving()
    {
        WorkingDir = Path.Combine(Environment.CurrentDirectory, "../../../../../artifacts/");
        Directory.CreateDirectory(WorkingDir);

        PoolingFactory = new PoolingFactory([
            typeof(ViciOne.Engine.DefaultPoolings.AndPooling).Assembly,
            typeof(Demo.Base).Assembly]);
    }

    protected static T Load<T>(string path)
        => JsonSerialization.Default.LoadFromFile<T>(path);

    protected static void Save(object instance, string path)
        => JsonSerialization.Default.Save(instance, path);
}
