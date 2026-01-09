using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimePersistentVariablesChangedObserver(IRuntimeFunctionBlock[] runtimeFunctionBlocks) :
    RuntimeElementsChangedObserver<IRuntimePersistentVariables, IRuntimePersistentVariable>(runtimeFunctionBlocks.Select(block => (block, block.PersistentVariables)))
{ }
