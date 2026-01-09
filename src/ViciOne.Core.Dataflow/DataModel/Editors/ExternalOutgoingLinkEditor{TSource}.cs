using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ExternalOutgoingLinkEditor<TSource>
{
    public ExternalOutgoingLink<TSource> Link { get; }

    public ExternalOutgoingLinkEditor(IConnector<TSource> source)
        => Link = new ExternalOutgoingLink<TSource>
        {
            Id = Guid.NewGuid(),
            Source = source,
        };

    public ExternalOutgoingLinkEditor(ExternalOutgoingLink<TSource> link)
        => Link = link;
}
