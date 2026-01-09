using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IContainer : IIdProvider
{
    IEnumerable<IContainer> Containers { get; }
    IEnumerable<IFunctionBlock> FunctionBlocks { get; }
    IEnumerable<IConnector> Inputs { get; }
    string Name { get; }
    IEnumerable<IConnector> Outputs { get; }
}
