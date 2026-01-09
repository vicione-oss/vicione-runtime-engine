using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel.Extensions;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ContainerEditor(Container container)
{
    public virtual Container Container { get; } = container;
    internal virtual ContainerEditor? ParentContainerEditor { get; set; }

    public ContainerEditor() : this(CreateModel()) { }

    protected virtual void AddConnector(Connector connector, List<Connector> listToAddTo)
    {
        if (IsConnectorExisting(connector))
            return;
        ValidateIsDirectChildConnector(connector);
        listToAddTo.Add(connector);
    }

    public virtual void AddContainer(ContainerEditor editor)
    {
        if (IsContainerExisting(editor.Container))
            return;
        ValidateContainerWillNotBeAddedToItself(editor);
        ValidateContainerNotAdded(editor);
        ValidateFunctionBlockOrContainerNameNotExisting(editor.Container.Name);
        Container.Containers.Add(editor.Container);
        editor.ParentContainerEditor = this;
    }

    public virtual void AddFunctionBlock(FunctionBlockEditor editor)
    {
        if (IsFunctionBlockExisting(editor.FunctionBlock))
            return;
        ValidateFunctionBlockNotAdded(editor);
        ValidateFunctionBlockOrContainerNameNotExisting(editor.FunctionBlock.Name);
        Container.FunctionBlocks.Add(editor.FunctionBlock);
        editor.ContainerEditor = this;
    }

    public virtual void AddInput(Connector input)
        => AddConnector(input, Container.Inputs);

    public virtual void AddOutput(Connector output)
        => AddConnector(output, Container.Outputs);

    private static Container CreateModel()
    {
        var container = new Container
        {
            Id = Guid.NewGuid(),
            Name = ContainerDefaults.Name,
        };
        return container;
    }

    protected virtual bool IsConnectorExisting(Connector connector)
        => Container.GetConnectors().Contains(connector);

    protected virtual bool IsContainerExisting(Container container)
        => Container.Containers.Contains(container);

    protected virtual bool IsContainerNameExisting(string name)
        => Container.Containers.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    protected virtual bool IsDirectChildConnector(Connector connector)
        => IsDirectChildConnector(Container.Containers, connector) ||
           IsDirectChildConnector(Container.FunctionBlocks, connector);

    protected virtual bool IsDirectChildConnector(IEnumerable<Container> containers, Connector connector)
        => containers.Any(x => x.GetConnectors().Contains(connector));

    protected virtual bool IsDirectChildConnector(IEnumerable<FunctionBlock> functionBlocks, Connector connector)
        => functionBlocks.Any(x => x.GetConnectors().Contains(connector));

    protected virtual bool IsFunctionBlockExisting(FunctionBlock functionBlock)
        => Container.FunctionBlocks.Contains(functionBlock);

    protected virtual bool IsFunctionBlockNameExisting(string name)
        => Container.FunctionBlocks.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public virtual bool RemoveContainer(ContainerEditor editor)
    {
        RemoveInputs(editor.Container.Inputs);
        RemoveOutputs(editor.Container.Outputs);
        bool returnValue = Container.Containers.Remove(editor.Container);
        editor.ParentContainerEditor = null;
        return returnValue;
    }

    public virtual bool RemoveFunctionBlock(FunctionBlockEditor editor)
    {
        RemoveInputs(editor.FunctionBlock.GetInputs().Cast<Connector>());
        RemoveOutputs(editor.FunctionBlock.GetOutputs().Cast<Connector>());
        bool returnValue = Container.FunctionBlocks.Remove(editor.FunctionBlock);
        editor.ContainerEditor = null;
        return returnValue;
    }

    public virtual bool RemoveInput(Connector input)
        => Container.Inputs.Remove(input);

    public virtual void RemoveInputs(IEnumerable<Connector> inputs)
    {
        foreach (Connector input in inputs)
            RemoveInput(input);
    }

    public virtual bool RemoveOutput(Connector output)
        => Container.Outputs.Remove(output);

    public virtual void RemoveOutputs(IEnumerable<Connector> outputs)
    {
        foreach (Connector output in outputs)
            RemoveOutput(output);
    }

    public virtual void SetName(string name)
       => Container.Name = ValidateName(name);

    protected virtual void ValidateContainerNotAdded(ContainerEditor editor)
    {
        if (editor.ParentContainerEditor is not null)
            throw new InvalidOperationException($"This {nameof(DataModel.Container)} was already added to a {nameof(DataModel.Container)}.");
    }

    protected virtual void ValidateContainerWillNotBeAddedToItself(ContainerEditor editor)
    {
        if (editor.Container == Container)
            throw new InvalidOperationException($"A {nameof(DataModel.Container)} cannot be added to itself.");
    }

    protected virtual void ValidateFunctionBlockNotAdded(FunctionBlockEditor editor)
    {
        if (editor.ContainerEditor is not null)
            throw new InvalidOperationException($"This {nameof(FunctionBlock)} was already added to a {nameof(DataModel.Container)}.");
    }

    protected internal virtual void ValidateFunctionBlockOrContainerNameNotExisting(string name)
    {
        if (IsContainerNameExisting(name))
            throw new InvalidOperationException($"A {nameof(DataModel.Container)} with this name is already existing.");
        if (IsFunctionBlockNameExisting(name))
            throw new InvalidOperationException($"A {nameof(FunctionBlock)} with this name is already existing.");
    }

    protected virtual void ValidateIsDirectChildConnector(Connector connector)
    {
        if (IsDirectChildConnector(connector) == false)
            throw new InvalidOperationException($"This {nameof(Connector)} is no (direct) child of this {nameof(DataModel.Container)} and therefore cannot be added.");
    }

    protected virtual string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));

        name = name.Trim();
        if (name.Equals(Container.Name, StringComparison.Ordinal) == false)
            ValidateNameNotExisting(name);

        return name;
    }

    protected virtual void ValidateNameNotExisting(string name)
        => ParentContainerEditor?.ValidateFunctionBlockOrContainerNameNotExisting(name);
}
