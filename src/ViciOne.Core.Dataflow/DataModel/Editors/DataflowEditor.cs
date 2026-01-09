using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class DataflowEditor : IDisposable
{
    private bool _disposed;

    public virtual Dataflow Dataflow { get; }
    public virtual ContainerEditor RootContainerEditor { get; protected set; }

    public DataflowEditor() : this(CreateModel()) { }

    public DataflowEditor(Dataflow dataflow)
    {
        Dataflow = dataflow;
        RootContainerEditor = new(Dataflow.RootContainer);
    }

    public virtual void AddExternalIncomingLink(ExternalIncomingLink link)
    {
        ValidateLinkDestinationExisting(link.Destination);
        AddExternalLink(link);
    }

    protected virtual void AddExternalLink(ExternalLink link)
    {
        if (IsLinkExisting(link))
            return;
        Dataflow.Links.Add(link);
    }

    public virtual void AddExternalOutgoingLink(ExternalOutgoingLink link)
    {
        ValidateLinkSourceExisting(link.Source, true);
        AddExternalLink(link);
    }

    public virtual void AddFunctionBlockDesign(FunctionBlockDesign design)
    {
        if (IsFunctionBlockDesignExisting(design))
            return;
        ValidateFunctionBlockDesignIdentifierNotExisting(design);
        Dataflow.FunctionBlockDesigns.Add(design);
    }

    public virtual void AddInternalLink(InternalLink link)
    {
        if (IsLinkExisting(link))
            return;
        ValidateLinkSourceExisting(link.Source, false);
        ValidateLinkDestinationExisting(link.Destination);
        ValidateInternalLinkWithSourceAndDestinationNotExisting(link);
        Dataflow.Links.Add(link);
    }

    private static Dataflow CreateModel()
        => new()
        {
            Id = Guid.NewGuid(),
            RootContainer = new Container()
            {
                Name = DataflowDefaults.RootContainerName,
                Id = Guid.NewGuid(),
            }
        };

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            FastCloner.FastCloner.ClearCache();

        _disposed = true;
    }

    protected virtual bool IsFunctionBlockDesignExisting(FunctionBlockDesign design)
        => Dataflow.FunctionBlockDesigns.Contains(design);

    protected virtual bool IsFunctionBlockDesignInUse(FunctionBlockDesign design)
        => IsFunctionBlockDesignExisting(design) && IsFunctionBlockDesignInUse(Dataflow.RootContainer, design);

    protected virtual bool IsFunctionBlockDesignInUse(Container container, FunctionBlockDesign design)
    {
        if (IsFunctionBlockDesignInUse(container.FunctionBlocks, design))
            return true;
        foreach (Container childContainer in container.Containers)
            if (IsFunctionBlockDesignInUse(childContainer, design))
                return true;
        return false;
    }

    protected virtual bool IsFunctionBlockDesignInUse(IEnumerable<FunctionBlock> functionBlocks, FunctionBlockDesign design)
       => functionBlocks.Any(x => x.Design == design);

    protected virtual bool IsLinkExisting(Link link)
        => Dataflow.Links.Contains(link);

    public virtual bool RemoveFunctionBlockDesign(FunctionBlockDesign design)
    {
        ValidateFunctionBlockDesignNotInUse(design);
        return Dataflow.FunctionBlockDesigns.Remove(design);
    }

    public virtual bool RemoveLink(Link link)
        => Dataflow.Links.Remove(link);

    protected virtual void ValidateFunctionBlockDesignNotInUse(FunctionBlockDesign design)
    {
        if (IsFunctionBlockDesignInUse(design))
            throw new InvalidOperationException($"This {nameof(FunctionBlockDesign)} is being used and therefore cannot be removed.");
    }

    protected virtual void ValidateFunctionBlockDesignIdentifierNotExisting(FunctionBlockDesign design)
    {
        if (Dataflow.FunctionBlockDesigns.Any(x => IsSameIdentifier(x, design)))
            throw new InvalidOperationException($"A {nameof(FunctionBlockDesign)} with the same identifier is already existing.");

        bool IsSameIdentifier(FunctionBlockDesign design1, FunctionBlockDesign design2)
            => GetIdentifier(design1).Equals(GetIdentifier(design2), StringComparison.OrdinalIgnoreCase);

        string GetIdentifier(FunctionBlockDesign functionBlockDesign)
            => $"{functionBlockDesign.Namespace}.{functionBlockDesign.Name}";
    }

    protected virtual void ValidateLinkSourceExisting(Connector sourceConnector, bool acceptInputAsSource)
    {
        foreach (IFunctionBlock block in Dataflow.RootContainer.GetFunctionBlocks(true))
            if ((acceptInputAsSource ? block.GetConnectors() : block.GetOutputs()).Contains(sourceConnector))
                return;
        throw new InvalidOperationException($"The {nameof(Connector)} supplied as link source is not existing in this {nameof(DataModel.Dataflow)}.");
    }

    protected virtual void ValidateLinkDestinationExisting(Connector destinationConnector)
    {
        foreach (IFunctionBlock block in Dataflow.RootContainer.GetFunctionBlocks(true))
            if (block.GetInputs().Contains(destinationConnector))
                return;
        throw new InvalidOperationException($"The {nameof(Connector)} supplied as link destination is not existing in this {nameof(DataModel.Dataflow)}.");
    }

    protected virtual void ValidateInternalLinkWithSourceAndDestinationNotExisting(InternalLink link)
    {
        if (Dataflow.Links
            .OfType<InternalLink>()
            .Any(x => x.Source == link.Source && x.Destination == link.Destination))
            throw new InvalidOperationException($"A {nameof(Link)} with the same source and destination is already existing.");
    }
}
