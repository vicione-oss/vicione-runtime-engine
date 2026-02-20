using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Engine.DefaultPoolings;

namespace GenerateFBDesigns;

internal static class Program
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0210:Konvertieren in Anweisungen der obersten Ebene", Justification = "Beispielprojekte einheitlich gestalten")]
    private static void Main(string[] args)
    {
        string dllDir = args[0];
        IEnumerable<Assembly> assemblies = Directory.GetFiles(dllDir, "*.dll", SearchOption.AllDirectories).Select(AssemblyLoadContext.Default.LoadFromAssemblyPath);

        PoolingFactory poolingFactory = new([typeof(SumPooling).Assembly]);
        IEnumerable<FunctionBlockDesignInfo> infos = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesigns(assemblies);
        IEnumerable<FunctionBlockDesign> designs = FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(infos, poolingFactory);

        Console.WriteLine("==== Infos ====");
        foreach (FunctionBlockDesignInfo info in infos)
            PrintInfo(info);

        Console.WriteLine();
        Console.WriteLine("==== Designs ====");
        foreach (FunctionBlockDesign design in designs)
            PrintInfo(design);
    }

    private static void PrintInfo(FunctionBlockDesignInfo info)
    {
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"ID: {info.Id}");
        Console.WriteLine($"Namespace: {info.Namespace}");
        Console.WriteLine($"Name: {info.Name}");
        if (info.RuntimeTypeName != null)
            Console.WriteLine($"RuntimeTypeName: {info.RuntimeTypeName}");
        if (info.DefaultCycleFrequency.HasValue)
            Console.WriteLine($"DefaultCycleFrequency: {info.DefaultCycleFrequency}");
        if (info.ValidRunModes != null)
            Console.WriteLine($"ValidRunModes: {info.ValidRunModes.Select(x => x.ToString()).Aggregate((x, y) => $"{x}, {y}")}");
        if (info.DefaultRunMode.HasValue)
            Console.WriteLine($"DefaultRunMode: {info.DefaultRunMode}");
        if (info.RuntimeDriverTypeName != null)
            Console.WriteLine($"RuntimeDriverTypeName: {info.RuntimeDriverTypeName}");
        Console.WriteLine($"FunctionBlockEnabled-Input-ID: {info.FunctionBlockEnabledInputId}");
        Console.WriteLine($"FunctionBlockEnabled-Output-ID: {info.FunctionBlockEnabledOutputId}");
        Console.WriteLine();
        Console.WriteLine("Settings:");
        foreach (SettingDesignInfo setting in info.Settings)
        {
            Console.WriteLine($"- {setting.Name} ({setting.Type.Name})");
            Console.WriteLine($"  ID: {setting.Id}");
            Console.WriteLine($"  Input-ID: {setting.InputId}");
            Console.WriteLine($"  Output-ID: {setting.OutputId}");
            if (setting.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {setting.DefaultValue}");
            foreach (object validator in setting.Validators)
                Console.WriteLine($"  Validator: {validator}");
        }
        Console.WriteLine();
        Console.WriteLine("Persistent Variables:");
        foreach (PersistentVariableDesignInfo variable in info.PersistentVariables)
        {
            Console.WriteLine($"- {variable.Name} ({variable.Type.Name})");
            Console.WriteLine($"  ID: {variable.Id}");
            if (variable.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {variable.DefaultValue}");
        }
        Console.WriteLine();
        Console.WriteLine("Inputs:");
        foreach (ConnectorDesignInfo input in info.Inputs)
        {
            Console.WriteLine($"- {input.Name} ({input.Type.Name})");
            Console.WriteLine($"  ID: {input.Id}");
            if (input.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {input.DefaultValue}");
            if (input.DefaultCrossSourcePoolingStrategy != null)
                Console.WriteLine($"  DefaultCrossSourcePoolingStrategy: {input.DefaultCrossSourcePoolingStrategy}");
            if (input.DefaultCrossSourceAggregatingPooling != null)
                Console.WriteLine($"  DefaultCrossSourceAggregatingPooling: {input.DefaultCrossSourceAggregatingPooling}");
            if (input.DefaultPerSourcePoolingStrategy != null)
                Console.WriteLine($"  DefaultPerSourcePoolingStrategy: {input.DefaultPerSourcePoolingStrategy}");
            if (input.DefaultPerSourceAggregatingPooling != null)
                Console.WriteLine($"  DefaultPerSourceAggregatingPooling: {input.DefaultPerSourceAggregatingPooling}");
            foreach (object validator in input.Validators)
                Console.WriteLine($"  Validator: {validator}");
        }
        Console.WriteLine();
        Console.WriteLine("Outputs:");
        foreach (ConnectorDesignInfo output in info.Outputs)
        {
            Console.WriteLine($"- {output.Name} ({output.Type.Name})");
            Console.WriteLine($"  ID: {output.Id}");
            if (output.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {output.DefaultValue}");
            foreach (object validator in output.Validators)
                Console.WriteLine($"  Validator: {validator}");
        }
        Console.WriteLine("------------------------------------------------------------");
    }

    private static void PrintInfo(FunctionBlockDesign design)
    {
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"ID: {design.Id}");
        Console.WriteLine($"Namespace: {design.Namespace}");
        Console.WriteLine($"Name: {design.Name}");
        if (design.RuntimeTypeName != null)
            Console.WriteLine($"RuntimeTypeName: {design.RuntimeTypeName}");
        Console.WriteLine($"DefaultCycleFrequency: {design.DefaultCycleFrequency}");
        Console.WriteLine($"ValidRunModes: {design.ValidRunModes.Select(x => x.ToString()).Aggregate((x, y) => $"{x}, {y}")}");
        Console.WriteLine($"DefaultRunMode: {design.DefaultRunMode}");
        if (design.RuntimeDriverTypeName != null)
            Console.WriteLine($"RuntimeDriverTypeName: {design.RuntimeDriverTypeName}");
        Console.WriteLine($"Enabled-Input-ID: {design.SystemInputs.First().Id}");
        Console.WriteLine($"Enabled-Output-ID: {design.SystemOutputs.First().Id}");
        Console.WriteLine();
        Console.WriteLine("Settings:");
        foreach (SettingDesign setting in design.Settings)
        {
            Console.WriteLine($"- {setting.Name} ({DataTypeCompatibilityValidator.DetermineValueType(setting.SettingType).Name})");
            Console.WriteLine($"  ID: {setting.Id}");
            Console.WriteLine($"  Input-ID: {setting.Input.Id}");
            Console.WriteLine($"  Output-ID: {setting.Output.Id}");
            if (setting.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {setting.DefaultValue}");
            foreach (IValidator validator in setting.Validators)
                Console.WriteLine($"  Validator: {validator}");
        }
        Console.WriteLine();
        Console.WriteLine("Persistent Variables:");
        foreach (PersistentVariableDesign variable in design.PersistentVariables)
        {
            Console.WriteLine($"- {variable.Name} ({DataTypeCompatibilityValidator.DetermineValueType(variable.PersistentVariableType).Name})");
            Console.WriteLine($"  ID: {variable.Id}");
            if (variable.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {variable.DefaultValue}");
        }
        Console.WriteLine();
        Console.WriteLine("Inputs:");
        foreach (ConnectorDesign input in design.ProcessDataInputs)
        {
            Console.WriteLine($"- {input.Name} ({DataTypeCompatibilityValidator.DetermineValueType(input.ConnectorType).Name})");
            Console.WriteLine($"  ID: {input.Id}");
            if (input.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {input.DefaultValue}");
            foreach (IValidator validator in input.Validators)
                Console.WriteLine($"  Validator: {validator}");
        }
        Console.WriteLine();
        Console.WriteLine("Outputs:");
        foreach (ConnectorDesign output in design.ProcessDataOutputs)
        {
            Console.WriteLine($"- {output.Name} ({DataTypeCompatibilityValidator.DetermineValueType(output.ConnectorType).Name})");
            Console.WriteLine($"  ID: {output.Id}");
            if (output.DefaultValue != null)
                Console.WriteLine($"  DefaultValue: {output.DefaultValue}");
            foreach (IValidator validator in output.Validators)
                Console.WriteLine($"  Validator: {validator}");
        }
        Console.WriteLine("------------------------------------------------------------");
    }
}
