using ViciOne.Core.Contracts.Runtime;

public abstract class RuntimePersistentVariablesTests : RuntimePeristableElementsTests
{
    private protected static IRuntimeFunctionBlock CreateBlockWithPersistentVariables(int persistentVariablesCount)
        => MockBlockWithEmptyElements<IRuntimePersistentVariables, IRuntimePersistentVariable>(persistentVariablesCount, (block) => block.PersistentVariables);

    private protected static DisposableList<IRuntimeFunctionBlock> CreateBlocksWithPersistentVariables(params int[] persistentVariablesCounts)
    {
        DisposableList<IRuntimeFunctionBlock> blocks = [];
        foreach (var persistentVariablesCount in persistentVariablesCounts)
            blocks.Add(CreateBlockWithPersistentVariables(persistentVariablesCount));
        return blocks;
    }
}
