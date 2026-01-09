using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeSettingsProviderTests : RuntimeSettingsTests
{
    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Justification = "Wird durch den Aufruf von Should().NotThrow() aufgerufen.")]
        public void CanDispose()
        {
            var provider = new RuntimeSettingsProvider([]);

            Action action = provider.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var provider = new RuntimeSettingsProvider([]);
            provider.Dispose();

            Action action = provider.Dispose;

            action.Should().NotThrow();
        }
    }

    public class GetAllSettings
    {
        [Fact]
        public void ReturnsAllSettings()
        {
            using var runtimeBlocks = CreateBlocksWithSettings(1, 0, 2);
            var allSettingsExpected = runtimeBlocks.ToDictionary(x => x, x => (IEnumerable<IRuntimeSetting>)x.Settings);
            using var provider = new RuntimeSettingsProvider([.. runtimeBlocks]);

            var allSettingsReturned = provider.GetAll();

            allSettingsReturned.Keys.Should().HaveCount(allSettingsExpected.Keys.Count);
            foreach (var blockSettings in allSettingsReturned)
            {
                allSettingsExpected.Should().ContainKey(blockSettings.Key);
                blockSettings.Value.Should().ContainAllButNotMore(allSettingsExpected[blockSettings.Key]);
            }
        }
    }
}
