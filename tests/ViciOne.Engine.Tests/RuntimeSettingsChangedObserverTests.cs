using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeSettingsChangedObserverTests : RuntimeSettingsTests
{
    public class Constructor
    {
        [Fact]
        public void AttachesToElementChangedPerBlock()
        {
            var block1 = Substitute.For<IRuntimeFunctionBlock>();
            var block2 = Substitute.For<IRuntimeFunctionBlock>();

            using var observer = new RuntimeSettingsChangedObserver([block1, block2]);

            block1.Settings.ReceivedWithAnyArgs(1).ElementChanged += null;
            block2.Settings.ReceivedWithAnyArgs(1).ElementChanged += null;
        }
    }

    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1, 1);
            var observer = new RuntimeSettingsChangedObserver([.. runtimeBlocks]);

            Action action = observer.Dispose;

            action.Should().NotThrow();
            foreach (var block in runtimeBlocks)
                block.Settings.ReceivedWithAnyArgs(1).ElementChanged -= null;
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1, 1);
            var observer = new RuntimeSettingsChangedObserver([.. runtimeBlocks]);
            observer.Dispose();

            Action action = observer.Dispose;

            action.Should().NotThrow();
            foreach (var block in runtimeBlocks)
                block.Settings.ReceivedWithAnyArgs(1).ElementChanged -= null;
        }
    }

    public class GetAndResetChangedSettings
    {
        [Fact]
        public void ReturnsChangedSettingsForAllAvailableBlocks()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1, 0, 2);
            var settingsToChange = runtimeBlocks.ToDictionary(x => x, x => x.Settings);
            using var observer = new RuntimeSettingsChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(settingsToChange);

            var changedSettings = observer.GetAndResetChanged();

            changedSettings.Should().BeEquivalentTo(settingsToChange);
        }
    }

    public class GetAndResetChangedSettings_IRuntimeFunctionBlock
    {
        [Fact]
        public void ReturnsChangedSettings()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1, 1);
            var settingsToChange = runtimeBlocks.ToDictionary(x => x, x => x.Settings);
            using var observer = new RuntimeSettingsChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(settingsToChange);
            var singleBlock = runtimeBlocks[1];

            var changedSettingsOfRuntimeBlock2 = observer.GetAndResetChanged(singleBlock);

            changedSettingsOfRuntimeBlock2.Should().Equal(settingsToChange[singleBlock]);
        }

        [Fact]
        public void ResetsChangedSettings()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1);
            var settingsToChange = runtimeBlocks.ToDictionary(x => x, x => x.Settings);
            using var observer = new RuntimeSettingsChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(settingsToChange);
            var singleBlock = runtimeBlocks[0];

            var resultFirstTry = observer.GetAndResetChanged(singleBlock);
            var resultSecondTry = observer.GetAndResetChanged(singleBlock);

            resultFirstTry.Should().NotBeNullOrEmpty();
            resultSecondTry.Should().BeEmpty();
        }

        [Fact]
        public void CanHandleNotChangedBlocks()
        {
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            using var observer = new RuntimeSettingsChangedObserver([]);

            var changedSettings = observer.GetAndResetChanged(runtimeBlock);

            changedSettings.Should().BeEmpty();
        }

        [Fact]
        public void CanHandleUnknownSetting()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1);
            var settingsToChange = runtimeBlocks.ToDictionary(x => x, x => x.Settings);
            using var observer = new RuntimeSettingsChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(settingsToChange);
            var unknownSetting = Substitute.For<IRuntimeSetting>();
            var singleBlock = runtimeBlocks[0];
            InvokeElementChanged(singleBlock.Settings, unknownSetting);

            var changedSettingsOfRuntimeBlock2 = observer.GetAndResetChanged(singleBlock);

            changedSettingsOfRuntimeBlock2.Should().Equal(settingsToChange[singleBlock]);
            changedSettingsOfRuntimeBlock2.Should().NotContain(unknownSetting);
        }

        [Fact]
        public void DontReturnAllChangedSettingsAtFirstCall()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1, 1);
            var settingsToChange = runtimeBlocks.ToDictionary(x => x, x => x.Settings);
            using var observer = new RuntimeSettingsChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(settingsToChange);
            var blockForFirstCall = runtimeBlocks[0];
            var blockForSecondCall = runtimeBlocks[1];

            var changedSettingsOfRuntimeBlock1 = observer.GetAndResetChanged(blockForFirstCall);
            var changedSettingsOfRuntimeBlock2 = observer.GetAndResetChanged(blockForSecondCall);

            changedSettingsOfRuntimeBlock1.Should().Equal(settingsToChange[blockForFirstCall]);
            changedSettingsOfRuntimeBlock2.Should().Equal(settingsToChange[blockForSecondCall]);
        }
    }

    private static void InvokeElementChanged(Dictionary<IRuntimeFunctionBlock, IRuntimeSettings> settingsToChange)
        => InvokeElementChanged<IRuntimeSettings, IRuntimeSetting>(settingsToChange);
}
