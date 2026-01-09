using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Serialization.Json;
using Generation = ViciOne.Core.Dataflow.DataModel.Generation;

namespace FunctionBlockDesignGenerator;

internal static class Program
{
    private class Options
    {
        [Option('i', "input", Required = true, HelpText = "Assembly which contains one or more RuntimeFunctionBlock implementations.")]
        public string InputFile { get; set; } = string.Empty;
        [Option('o', "output", Required = true, HelpText = "Output directory.")]
        public string OutputDirectory { get; set; } = string.Empty;
        [Option('p', "paths", Required = false, HelpText = "Specifies directories which contains additional assemblies e.g. for poolings or validators.")]
        public IEnumerable<string> AdditionalAssemblyPaths { get; set; } = [];
    }

    public static int Main(string[] args)
        => Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                RunAndReturnExitCode,
                _ => 1);

    private static void CreateDirectoryIfNecessary(string path)
    {
        if (Directory.Exists(path) == false)
            Directory.CreateDirectory(path);
    }

    private static string CreateOutputFilePath(string directoryPath, FunctionBlockDesign design)
        => Path.Combine(directoryPath, $"{design.Namespace}.{design.Name}.json");

    private static IEnumerable<Assembly> LoadAdditionalAssemblies(IEnumerable<string> paths)
    {
        foreach (string directory in paths)
            foreach (string file in Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories))
                yield return Assembly.LoadFrom(file);
    }

    private static int RunAndReturnExitCode(Options options)
    {
        FunctionBlockDesign[] designs;
        try
        {
            var assembly = Assembly.LoadFrom(options.InputFile);
            Assembly[] additionalAssemblies = [.. LoadAdditionalAssemblies(options.AdditionalAssemblyPaths)];
            PoolingFactory poolingFactory = new(additionalAssemblies);
            IEnumerable<Generation.IntermediateModel.FunctionBlockDesignInfo> infos = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesigns([assembly]);
            designs = [.. Generation.FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(infos, poolingFactory)];
            CreateDirectoryIfNecessary(options.OutputDirectory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occured: {ex}");
            return 2;
        }

        foreach (FunctionBlockDesign design in designs)
        {
            try { JsonSerialization.Default.Save(design, CreateOutputFilePath(options.OutputDirectory, design)); }
            catch (Exception ex) { Console.Error.WriteLine($"An error occured: {ex}"); return 3; }
        }

        return 0;
    }
}
