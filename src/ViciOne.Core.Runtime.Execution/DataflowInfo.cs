using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Execution;

public class DataflowInfo(IDataflow dataflow, List<InputConnectorInfo> inputs, List<OutputConnectorInfo> outputs, List<ISetting> settings, List<IPersistentVariable> persistentVariables)
{
    public IDataflow Dataflow { get; } = dataflow;
    public List<InputConnectorInfo> Inputs { get; } = inputs;
    public List<OutputConnectorInfo> Outputs { get; } = outputs;
    public List<ISetting> Settings { get; } = settings;
    public List<IPersistentVariable> PersistentVariables { get; } = persistentVariables;
}
