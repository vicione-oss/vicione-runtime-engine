using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeFunctionBlockInitializerTests
{
    public class InitializeInstancesI : RuntimeFunctionBlockInitializerTests
    {
        [Theory]
        [MemberData(nameof(ArgumentsFor_CanInitialize))]
        public void CanInitialize(IDataflow dataflow, IEnumerable<IRuntimeFunctionBlock> instances, IEnumerable<IRuntimeFunctionBlock> expectedInitialized, IEnumerable<IRuntimeFunctionBlock> expectedFailed)
        {
            var logger = new TestLogger();
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);

            (var initialized, var failed) =
                RuntimeFunctionBlockInitializer.Default.InitializeInstancesI(instances, nameCache, logger);

            initialized.Should().BeEquivalentTo(expectedInitialized);
            failed.Keys.Should().BeEquivalentTo(expectedFailed);
            foreach (var driver in instances)
                driver.Received().InitializeI();
            logger.GetLoggedMessages(LogLevel.Error).Should().AllSatisfy(x => x.Contains("phase 1", StringComparison.OrdinalIgnoreCase));
        }
    }

    public class InitializeInstancesII : RuntimeFunctionBlockInitializerTests
    {
        [Theory]
        [MemberData(nameof(ArgumentsFor_CanInitialize))]
        public void CanInitialize(IDataflow dataflow, IEnumerable<IRuntimeFunctionBlock> instances, IEnumerable<IRuntimeFunctionBlock> expectedInitialized, IEnumerable<IRuntimeFunctionBlock> expectedFailed)
        {
            var logger = new TestLogger();
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);

            (var initialized, var failed) =
                RuntimeFunctionBlockInitializer.Default.InitializeInstancesII(instances, nameCache, logger);

            initialized.Should().BeEquivalentTo(expectedInitialized);
            failed.Keys.Should().BeEquivalentTo(expectedFailed);
            foreach (var driver in instances)
                driver.Received().InitializeII();
            logger.GetLoggedMessages(LogLevel.Error).Should().AllSatisfy(x => x.Contains("phase 2", StringComparison.OrdinalIgnoreCase));
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Sufficient for the functionality to be tested here")]
    public static TheoryData<IDataflow, IEnumerable<IRuntimeFunctionBlock>, IEnumerable<IRuntimeFunctionBlock>, IEnumerable<IRuntimeFunctionBlock>> ArgumentsFor_CanInitialize()
    {
        var successful1 = Substitute.For<IRuntimeFunctionBlock>();
        var successful2 = Substitute.For<IRuntimeFunctionBlock>();
        var failed = Substitute.For<IRuntimeFunctionBlock>();
        failed
            .When(x => x.InitializeI())
            .Do(x => throw new Exception());
        failed
           .When(x => x.InitializeII())
           .Do(x => throw new Exception());
        var dataflow = Substitute.For<IDataflow>();
        IFunctionBlock[] fbs =
        [
            successful1.FunctionBlock,
            successful2.FunctionBlock,
            failed.FunctionBlock,
        ];
        dataflow.RootContainer.FunctionBlocks.Returns(fbs);

        return new() { { dataflow, [successful1, successful2, failed], [successful1, successful2], [failed] } };
    }
}
