using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public sealed class OutputLinkGroupFactory : GenericFactory
{
    public OutputLinkGroupFactory() : base(GetGenericCreateMethod()) { }

    public IOutputLinkGroup Create(IRuntimeConnector connector, IEnumerable<IRuntimeLink> links)
        => Create<IOutputLinkGroup>(DataTypeCompatibilityValidator.DetermineValueType(connector), [connector, links]);

    private static OutputLinkGroup<TOutput> Create<TOutput>(IRuntimeConnector<TOutput> connector, IEnumerable<IRuntimeLink> links)
        => new(connector, [.. links]);

    private static MethodInfo GetGenericCreateMethod()
        => typeof(OutputLinkGroupFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
