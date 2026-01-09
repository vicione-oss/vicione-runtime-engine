using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimePersistentVariablesProvider(IRuntimeFunctionBlock[] runtimeFunctionBlocks) :
    RuntimeElementsProvider<IRuntimePersistentVariables, IRuntimePersistentVariable>(runtimeFunctionBlocks.Select(block => (block, block.PersistentVariables)))
{ }
