using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimeSettingsChangedObserver(IRuntimeFunctionBlock[] runtimeFunctionBlocks) :
    RuntimeElementsChangedObserver<IRuntimeSettings, IRuntimeSetting>(runtimeFunctionBlocks.Select(block => (block, block.Settings)))
{ }
