using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public interface ILinkGroup
{
    IRuntimeConnector Connector { get; }
    IEnumerable<IRuntimeLink> Links { get; }
}
