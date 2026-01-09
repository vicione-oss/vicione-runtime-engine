using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IDataflow : IIdProvider
{
    IEnumerable<IFunctionBlockDesign> FunctionBlockDesigns { get; }
    IEnumerable<ILink> Links { get; }
    IContainer RootContainer { get; }
}
