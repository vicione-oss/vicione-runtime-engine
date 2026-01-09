using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Filters;

public static class ConnectorFilter
{
    public static TConnector? GetConnectorByName<TConnector>(IEnumerable<TConnector> connectors, string name) where TConnector : IConnector
        => connectors.FirstOrDefault(x => string.Equals(x.Design.Name, name, StringComparison.OrdinalIgnoreCase));

    public static TConnector? GetConnectorById<TConnector>(IEnumerable<TConnector> connectors, Guid id) where TConnector : IConnector
        => connectors.FirstOrDefault(x => x.Id == id);
}
