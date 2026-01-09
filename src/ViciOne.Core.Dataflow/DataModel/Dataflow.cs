using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public class Dataflow : IDataflow
{
    public List<FunctionBlockDesign> FunctionBlockDesigns { get; set; } = [];
    IEnumerable<IFunctionBlockDesign> IDataflow.FunctionBlockDesigns => FunctionBlockDesigns;
    public Guid Id { get; set; }
    public List<Link> Links { get; set; } = [];
    IEnumerable<ILink> IDataflow.Links => Links;
    public Container RootContainer { get; set; } = new Container();
    IContainer IDataflow.RootContainer => RootContainer;
}
