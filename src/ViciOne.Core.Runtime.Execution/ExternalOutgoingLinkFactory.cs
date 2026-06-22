using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Engine;

namespace ViciOne.Core.Runtime.Execution;

internal sealed class ExternalOutgoingLinkFactory() : GenericFactory(GetGenericCreateMethod())
{
    public ExternalOutgoingLink CreateAndAddToDataflow(Connector source, DataflowEditor dataflowEditor)
        => Create<ExternalOutgoingLink>(DataTypeCompatibilityValidator.DetermineValueType(source), [source, dataflowEditor]);

    private static ExternalOutgoingLink<TSource> CreateAndAddToDataflow<TSource>(IConnector<TSource> source, DataflowEditor dataflowEditor)
    {
        ExternalOutgoingLink<TSource> link = new ExternalOutgoingLinkEditor<TSource>(source).Link;
        dataflowEditor.AddExternalOutgoingLink(link);
        return link;
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(ExternalOutgoingLinkFactory).GetMethod(nameof(CreateAndAddToDataflow), BindingFlags.NonPublic | BindingFlags.Static)!;
}
