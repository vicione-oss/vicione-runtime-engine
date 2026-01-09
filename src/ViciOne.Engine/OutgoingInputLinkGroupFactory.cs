using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class OutgoingInputLinkGroupFactory : GenericFactory
{
    public OutgoingInputLinkGroupFactory() : base(GetGenericCreateMethod()) { }

    public IOutgoingInputLinkGroup Create(IRuntimeConnector connector, IEnumerable<IRuntimeLink> links)
        => Create<IOutgoingInputLinkGroup>(DataTypeCompatibilityValidator.DetermineValueType(connector), [connector, links]);

    private static OutgoingInputLinkGroup<TInput> Create<TInput>(IRuntimeConnector<TInput> connector, IEnumerable<IRuntimeLink> links)
        => new(connector, [.. links]);

    private static MethodInfo GetGenericCreateMethod()
        => typeof(OutgoingInputLinkGroupFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
