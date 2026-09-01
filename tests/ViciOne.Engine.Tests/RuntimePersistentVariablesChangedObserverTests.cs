using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimePersistentVariablesChangedObserverTests : RuntimePersistentVariablesTests
{
    public class Constructor
    {
        [Fact]
        public void AttachesToElelementChangedPerBlock()
        {
            var block1 = Substitute.For<IRuntimeFunctionBlock>();
            var block2 = Substitute.For<IRuntimeFunctionBlock>();

            using var observer = new RuntimePersistentVariablesChangedObserver([block1, block2]);

            block1.PersistentVariables.ReceivedWithAnyArgs(1).ElementChanged += null;
            block2.PersistentVariables.ReceivedWithAnyArgs(1).ElementChanged += null;
        }
    }

    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1, 1);
            var observer = new RuntimePersistentVariablesChangedObserver([.. runtimeBlocks]);

            Action action = observer.Dispose;

            action.Should().NotThrow();
            foreach (var block in runtimeBlocks)
                block.PersistentVariables.ReceivedWithAnyArgs(1).ElementChanged -= null;
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1, 1);
            var observer = new RuntimePersistentVariablesChangedObserver([.. runtimeBlocks]);
            observer.Dispose();

            Action action = observer.Dispose;

            action.Should().NotThrow();
            foreach (var block in runtimeBlocks)
                block.PersistentVariables.ReceivedWithAnyArgs(1).ElementChanged -= null;
        }
    }

    public class GetAndResetChangedPersistentVariables
    {
        [Fact]
        public void ReturnsChangedPersistentVariablesForAllAvailableBlocks()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1, 0, 2);
            var persistentVariablesToChange = runtimeBlocks.ToDictionary(x => x, x => x.PersistentVariables);
            using var observer = new RuntimePersistentVariablesChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(persistentVariablesToChange);

            var changedPersistentVariables = observer.GetAndResetChanged();

            changedPersistentVariables.Should().HaveCount(persistentVariablesToChange.Count);
            foreach (var pair in persistentVariablesToChange)
            {
                var changed = changedPersistentVariables[pair.Key];
                changed.Should().HaveCount(pair.Value.Count());
                foreach (var persistentVariable in pair.Value)
                    changed.Should().Contain(persistentVariable);
            }
        }
    }

    public class GetAndResetChangedPersistentVariables_IRuntimeFunctionBlock
    {
        [Fact]
        public void ReturnsChangedPersistentVariables()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1, 1);
            var persistentVariablesToChange = runtimeBlocks.ToDictionary(x => x, x => x.PersistentVariables);
            using var observer = new RuntimePersistentVariablesChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(persistentVariablesToChange);
            var singleBlock = runtimeBlocks[1];

            var changedPersistentVariablesOfRuntimeBlock2 = observer.GetAndResetChanged(singleBlock);

            changedPersistentVariablesOfRuntimeBlock2.Should().Equal(persistentVariablesToChange[singleBlock]);
        }

        [Fact]
        public void ResetsChangedPersistentVariables()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1);
            var persistentVariablesToChange = runtimeBlocks.ToDictionary(x => x, x => x.PersistentVariables);
            using var observer = new RuntimePersistentVariablesChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(persistentVariablesToChange);
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
            using var observer = new RuntimePersistentVariablesChangedObserver([]);

            var changedPersistentVariables = observer.GetAndResetChanged(runtimeBlock);

            changedPersistentVariables.Should().BeEmpty();
        }

        [Fact]
        public void CanHandleUnknownPersistentVariable()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1);
            var persistentVariablesToChange = runtimeBlocks.ToDictionary(x => x, x => x.PersistentVariables);
            using var observer = new RuntimePersistentVariablesChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(persistentVariablesToChange);
            var unknownPersistentVariable = Substitute.For<IRuntimePersistentVariable>();
            var singleBlock = runtimeBlocks[0];
            InvokeElementChanged(singleBlock.PersistentVariables, unknownPersistentVariable);

            var changedPersistentVariablesOfRuntimeBlock2 = observer.GetAndResetChanged(singleBlock);

            changedPersistentVariablesOfRuntimeBlock2.Should().Equal(persistentVariablesToChange[singleBlock]);
            changedPersistentVariablesOfRuntimeBlock2.Should().NotContain(unknownPersistentVariable);
        }

        [Fact]
        public void DontReturnAllChangedPersistentVariablesAtFirstCall()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1, 1);
            var persistentVariablesToChange = runtimeBlocks.ToDictionary(x => x, x => x.PersistentVariables);
            using var observer = new RuntimePersistentVariablesChangedObserver([.. runtimeBlocks]);
            InvokeElementChanged(persistentVariablesToChange);
            var blockForFirstCall = runtimeBlocks[0];
            var blockForSecondCall = runtimeBlocks[1];

            var changedPersistentVariablesOfRuntimeBlock1 = observer.GetAndResetChanged(blockForFirstCall);
            var changedPersistentVariablesOfRuntimeBlock2 = observer.GetAndResetChanged(blockForSecondCall);

            changedPersistentVariablesOfRuntimeBlock1.Should().Equal(persistentVariablesToChange[blockForFirstCall]);
            changedPersistentVariablesOfRuntimeBlock2.Should().Equal(persistentVariablesToChange[blockForSecondCall]);
        }
    }

    private static void InvokeElementChanged(Dictionary<IRuntimeFunctionBlock, IRuntimePersistentVariables> persistentVariablesToChange)
        => InvokeElementChanged<IRuntimePersistentVariables, IRuntimePersistentVariable>(persistentVariablesToChange);
}
