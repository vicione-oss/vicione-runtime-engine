using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Engine;

namespace ViciOne.Core.Runtime.Execution;

internal sealed class ExternalIncomingLinkFactory() : GenericFactory(GetGenericCreateMethod())
{
    public ExternalIncomingLink CreateAndAddToDataflow(Connector destination, DataflowEditor dataflowEditor)
        => Create<ExternalIncomingLink>([destination, dataflowEditor],
            DataTypeCompatibilityValidator.DetermineLinkValueType(destination),
            DataTypeCompatibilityValidator.DetermineValueType(destination));

    private static ExternalIncomingLink<TValue, TValue, TDestination> CreateAndAddToDataflow<TValue, TDestination>(ConnectorInput<TDestination> destination, DataflowEditor dataflowEditor)
    {
        ExternalIncomingLink<TValue, TValue, TDestination> link = new ExternalIncomingLinkEditor<TValue, TValue, TDestination>(destination).Link;
        dataflowEditor.AddExternalIncomingLink(link);
        return link;
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(ExternalIncomingLinkFactory).GetMethod(nameof(CreateAndAddToDataflow), BindingFlags.NonPublic | BindingFlags.Static)!;
}
