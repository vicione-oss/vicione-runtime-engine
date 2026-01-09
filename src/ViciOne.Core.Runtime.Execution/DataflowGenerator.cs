using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.DataModel.Extensions;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.Pooling;

namespace ViciOne.Core.Runtime.Execution;

public static class DataflowGenerator
{
    public static DataflowInfo GenerateDataflow(RelevantAssemblies assemblies)
        => GenerateDataflow(assemblies, x => x);

    public static DataflowInfo GenerateDataflow(RelevantAssemblies assemblies, Func<IEnumerable<FunctionBlockDesign>, FunctionBlockDesign> selectDesign)
        => selectDesign == null
            ? GenerateDataflow(assemblies, x => x)
            : GenerateDataflow(assemblies, x => Enumerable.Repeat(selectDesign(x), 1));

    public static DataflowInfo GenerateDataflow(RelevantAssemblies assemblies, Func<IEnumerable<FunctionBlockDesign>, IEnumerable<FunctionBlockDesign>> filterDesigns)
    {
        List<FunctionBlockDesign> generatedDesigns = GenerateFunctionBlockDesigns(assemblies);
        IEnumerable<FunctionBlockDesign> filteredDesigns = filterDesigns == null ? generatedDesigns : filterDesigns(generatedDesigns);
        DataflowInfo dataflowInfo = GenerateDataflow(filteredDesigns);
        return dataflowInfo;
    }

    private static List<FunctionBlockDesign> GenerateFunctionBlockDesigns(RelevantAssemblies assemblies)
    {
        var poolingFactory = new PoolingFactory(assemblies.AdditionalAssemblies);
        IEnumerable<Dataflow.DataModel.Generation.IntermediateModel.FunctionBlockDesignInfo> infos = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesigns([assemblies.FbAssembly]);
        var generatedDesigns = FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(infos, poolingFactory).ToList();
        return generatedDesigns;
    }

    public static DataflowInfo GenerateDataflow(IEnumerable<FunctionBlockDesign> designs)
    {
        var settings = new List<ISetting>();
        var variables = new List<IPersistentVariable>();
        var inputs = new List<InputConnectorInfo>();
        var outputs = new List<OutputConnectorInfo>();

        using var dataflowEditor = new DataflowEditor();

        using ExternalIncomingLinkFactory incomingLinkFactory = new();
        using ExternalOutgoingLinkFactory outgoingLinkFactory = new();

        foreach (FunctionBlockDesign design in designs)
        {
            dataflowEditor.AddFunctionBlockDesign(design);
            var functionBlockEditor = new FunctionBlockEditor(design);
            dataflowEditor.RootContainerEditor.AddFunctionBlock(functionBlockEditor);
            foreach (Setting setting in functionBlockEditor.FunctionBlock.Settings)
                settings.Add(setting);
            foreach (PersistentVariable variable in functionBlockEditor.FunctionBlock.PersistentVariables)
                variables.Add(variable);
            foreach (Connector input in functionBlockEditor.FunctionBlock.GetInputs())
            {
                ExternalIncomingLink incomingLink = incomingLinkFactory.CreateAndAddToDataflow(input, dataflowEditor);
                ExternalOutgoingLink outgoingLink = outgoingLinkFactory.CreateAndAddToDataflow(input, dataflowEditor);
                var info = new InputConnectorInfo(input, incomingLink, outgoingLink);
                inputs.Add(info);
            }
            foreach (Connector output in functionBlockEditor.FunctionBlock.GetOutputs())
            {
                ExternalOutgoingLink link = outgoingLinkFactory.CreateAndAddToDataflow(output, dataflowEditor);
                var info = new OutputConnectorInfo(output, link);
                outputs.Add(info);
            }
        }

        return new DataflowInfo(dataflowEditor.Dataflow, inputs, outputs, settings, variables);
    }
}
