using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

[DebuggerDisplay("{Name,nq}")]
public class Container : IContainer
{
    public List<Container> Containers { get; set; } = [];
    IEnumerable<IContainer> IContainer.Containers => Containers;
    public List<FunctionBlock> FunctionBlocks { get; set; } = [];
    IEnumerable<IFunctionBlock> IContainer.FunctionBlocks => FunctionBlocks;
    public Guid Id { get; set; }
    public List<Connector> Inputs { get; set; } = [];
    IEnumerable<IConnector> IContainer.Inputs => Inputs;
    public string Name { get; set; } = string.Empty;
    public List<Connector> Outputs { get; set; } = [];
    IEnumerable<IConnector> IContainer.Outputs => Outputs;
}
