using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimePersistentVariablesProviderTests : RuntimePersistentVariablesTests
{
    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var provider = new RuntimePersistentVariablesProvider([]);

            Action action = provider.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var provider = new RuntimePersistentVariablesProvider([]);
            provider.Dispose();

            Action action = provider.Dispose;

            action.Should().NotThrow();
        }
    }

    public class GetAllPersistentVariables
    {
        [Fact]
        public void ReturnsAllPersistentVariables()
        {
            using var runtimeBlocks = CreateBlocksWithPersistentVariables(1, 0, 2);
            var allPersistentVariablesExpected = runtimeBlocks.ToDictionary(x => x, x => (IEnumerable<IRuntimePersistentVariable>)x.PersistentVariables);
            using var provider = new RuntimePersistentVariablesProvider([.. runtimeBlocks]);

            var allPersistentVariablesReturned = provider.GetAll();

            allPersistentVariablesReturned.Keys.Should().HaveCount(allPersistentVariablesExpected.Keys.Count);
            foreach (var blockPersistentVariables in allPersistentVariablesReturned)
            {
                allPersistentVariablesExpected.Should().ContainKey(blockPersistentVariables.Key);
                blockPersistentVariables.Value.Should().ContainAllButNotMore(allPersistentVariablesExpected[blockPersistentVariables.Key]);
            }
        }
    }
}
