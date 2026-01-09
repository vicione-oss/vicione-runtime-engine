using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts;

public static class NameGenerator
{
    public static string ExternalIdentifier { get; } = "External";
    public static string LinkSourceTargetSeparator { get; } = "->";
    public static string NameSeparator { get; } = ".";
    public static string PersistentVariablesIdentifier { get; } = "PersistentVariables";
    public static string ProcessDataInputsIdentifier { get; } = "Inputs.ProcessData";
    public static string ProcessDataOutputsIdentifier { get; } = "Outputs.ProcessData";
    public static string SettingsIdentifier { get; } = "Settings";
    public static string SettingsInputsIdentifier { get; } = "Inputs.Settings";
    public static string SettingsOutputsIdentifier { get; } = "Outputs.Settings";
    public static string SystemInputsIdentifier { get; } = "Inputs.System";
    public static string SystemOutputsIdentifier { get; } = "Outputs.System";

    public static Dictionary<Guid, string> CreateUniqueNames(IDataflow dataflow)
    {
        ArgumentNullException.ThrowIfNull(dataflow);

        Dictionary<Guid, string> names = new()
        {
            [dataflow.RootContainer.Id] = dataflow.RootContainer.Name
        };
        CreateNames(dataflow.RootContainer.Name, dataflow.RootContainer.Containers, names);
        CreateNames(dataflow.RootContainer.Name, dataflow.RootContainer.FunctionBlocks, names);
        CreateLinkNames(dataflow.Links, names);

        return names;
    }

    private static void CreateLinkNames(IEnumerable<ILink> links, Dictionary<Guid, string> names)
    {
        links = [.. links];
        foreach (IInternalLink link in links.OfType<IInternalLink>())
            names.Add(link.Id, $"{names[link.Source.Id]}{LinkSourceTargetSeparator}{names[link.Destination.Id]}");
        foreach (IExternalIncomingLink link in links.OfType<IExternalIncomingLink>())
            names.Add(link.Id, $"{ExternalIdentifier}{LinkSourceTargetSeparator}{names[link.Destination.Id]}");
        foreach (IExternalOutgoingLink link in links.OfType<IExternalOutgoingLink>())
            names.Add(link.Id, $"{names[link.Source.Id]}{LinkSourceTargetSeparator}{ExternalIdentifier}");
    }

    private static void CreateNames(string parent, IEnumerable<IContainer> containers, IDictionary<Guid, string> names)
    {
        foreach (IContainer container in containers)
        {
            string name = CreateName(parent, container.Name);
            names[container.Id] = name;

            CreateNames(name, container.Containers, names);
            CreateNames(name, container.FunctionBlocks, names);
        }
    }

    private static void CreateNames(string parent, IEnumerable<IFunctionBlock> functionBlocks, IDictionary<Guid, string> names)
    {
        foreach (IFunctionBlock block in functionBlocks)
        {
            string name = CreateName(parent, block.Name);
            names[block.Id] = name;

            CreateNames(CreateName(name, SystemInputsIdentifier), block.SystemInputs, names);
            CreateNames(CreateName(name, SystemOutputsIdentifier), block.SystemOutputs, names);
            CreateNames(CreateName(name, SettingsInputsIdentifier), block.SettingsInputs, names);
            CreateNames(CreateName(name, SettingsOutputsIdentifier), block.SettingsOutputs, names);
            CreateNames(CreateName(name, ProcessDataInputsIdentifier), block.ProcessDataInputs, names);
            CreateNames(CreateName(name, ProcessDataOutputsIdentifier), block.ProcessDataOutputs, names);
            CreateNames(CreateName(name, SettingsIdentifier), block.Settings, names);
            CreateNames(CreateName(name, PersistentVariablesIdentifier), block.PersistentVariables, names);
        }
    }

    private static void CreateNames(string parent, IEnumerable<IConnector> connectors, IDictionary<Guid, string> names)
        => CreateNames(parent, connectors.Select(x => (x.Id, x.Design.Name)), names);

    private static void CreateNames(string parent, IEnumerable<ISetting> settings, IDictionary<Guid, string> names)
        => CreateNames(parent, settings.Select(x => (x.Id, x.Design.Name)), names);

    private static void CreateNames(string parent, IEnumerable<IPersistentVariable> variables, IDictionary<Guid, string> names)
        => CreateNames(parent, variables.Select(x => (x.Id, x.Design.Name)), names);

    private static void CreateNames(string parent, IEnumerable<(Guid Id, string Name)> currentElements, IDictionary<Guid, string> names)
    {
        foreach ((Guid elementId, string elementName) in currentElements)
        {
            string name = CreateName(parent, elementName);
            names[elementId] = name;
        }
    }

    private static string CreateName(string parent, string current)
        => $"{parent}{NameSeparator}{current}";
}
