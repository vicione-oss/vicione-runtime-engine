using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeDriverInitializerTests
{
    public class InitializeInstancesI : RuntimeDriverInitializerTests
    {
        [Theory]
        [MemberData(nameof(ArgumentsFor_CanInitialize))]
        public void CanInitialize(IEnumerable<IRuntimeDriver> instances, IEnumerable<IRuntimeDriver> expectedInitialized, IEnumerable<IRuntimeDriver> expectedFailed)
        {
            var logger = new TestLogger();
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());

            (var initialized, var failed) =
                RuntimeDriverInitializer.Default.InitializeInstancesI(instances, nameCache, logger);

            initialized.Should().ContainAllButNotMore(expectedInitialized);
            failed.Keys.Should().ContainAllButNotMore(expectedFailed);
            foreach (var driver in instances)
                driver.Received().InitializeI();
            logger.GetLoggedMessages(LogLevel.Error).Should().AllSatisfy(x => x.Contains("phase 1", StringComparison.OrdinalIgnoreCase));
        }
    }

    public class InitializeInstancesII : RuntimeDriverInitializerTests
    {
        [Theory]
        [MemberData(nameof(ArgumentsFor_CanInitialize))]
        public void CanInitialize(IEnumerable<IRuntimeDriver> instances, IEnumerable<IRuntimeDriver> expectedInitialized, IEnumerable<IRuntimeDriver> expectedFailed)
        {
            var logger = new TestLogger();
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());

            (var initialized, var failed) =
                RuntimeDriverInitializer.Default.InitializeInstancesII(instances, nameCache, logger);

            initialized.Should().ContainAllButNotMore(expectedInitialized);
            failed.Keys.Should().ContainAllButNotMore(expectedFailed);
            foreach (var driver in instances)
                driver.Received().InitializeII();
            logger.GetLoggedMessages(LogLevel.Error).Should().AllSatisfy(x => x.Contains("phase 2", StringComparison.OrdinalIgnoreCase));
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Keine reservierten Ausnahmetypen auslösen", Justification = "Ausreichend für die hier zu testende Funktionalität")]
    public static TheoryData<IEnumerable<IRuntimeDriver>, IEnumerable<IRuntimeDriver>, IEnumerable<IRuntimeDriver>> ArgumentsFor_CanInitialize
    {
        get
        {
            var successful1 = Substitute.For<IRuntimeDriver>();
            var successful2 = Substitute.For<IRuntimeDriver>();
            var failed = Substitute.For<IRuntimeDriver>();
            failed
                .When(x => x.InitializeI())
                .Do(x => throw new Exception());
            failed
               .When(x => x.InitializeII())
               .Do(x => throw new Exception());

            return new() { { [successful1, successful2, failed], [successful1, successful2], [failed] }, };
        }
    }
}
