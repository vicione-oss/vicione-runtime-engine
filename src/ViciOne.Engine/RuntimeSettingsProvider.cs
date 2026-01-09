using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class RuntimeSettingsProvider(IRuntimeFunctionBlock[] runtimeFunctionBlocks) :
    RuntimeElementsProvider<IRuntimeSettings, IRuntimeSetting>(runtimeFunctionBlocks.Select(block => (block, block.Settings)))
{ }
