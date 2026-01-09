using DummyBlocksA;
using DummyBlocksB;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeFunctionBlockFactoryTests
{
    public class Constructor
    {
        [Fact]
        public void CanCreateInstance()
        {
            Action action = () => _ = new RuntimeFunctionBlockFactory(GetAssemblies());

            action.Should().NotThrow();
        }
    }

    public class CreateInstances
    {
        [Fact]
        public void CanCreateInstances()
        {
            var blocksExpectedToSucceed = CreateGoodFunctionBlocks().ToList();
            var blocksExpectedToFail = CreateBadFunctionBlocks().ToList();
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            using var loggerFactory = new TestLoggerFactory();
            var logger = new TestLogger();
            var factory = new RuntimeFunctionBlockFactory(GetAssemblies());

            var (instances, failedDesigns) = factory.CreateInstances(blocksExpectedToSucceed.Concat(blocksExpectedToFail), uniqueNameProvider, loggerFactory, logger);

            blocksExpectedToSucceed.Should().ContainAllButNotMore(instances.Select(x => x.BlockAndDriver));
            foreach (var blockAndDriver in blocksExpectedToSucceed)
            {
                var runtimeBlock = instances.First(x => x.BlockAndDriver == blockAndDriver).RuntimeBlock;
                runtimeBlock.GetType().FullName.Should().Be(blockAndDriver.Block.Design.GetRuntimeTypeName());
            }

            blocksExpectedToFail.Select(x => x.Block.Design).Distinct().Should().ContainAllButNotMore(failedDesigns.Select(x => x.Design));
            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            foreach ((var block, var driver, _) in blocksExpectedToFail)
            {
                var exception = failedDesigns.First(x => x.Design == block.Design).Exception;
                errorMessages
                    .FirstOrDefault(x =>
                        x.Contains(block.Design.Namespace, StringComparison.Ordinal) &&
                        x.Contains(block.Design.Name, StringComparison.Ordinal) &&
                        x.Contains(exception.Message, StringComparison.Ordinal))
                    .Should().NotBeNull();
            }
        }

        [Fact]
        public void CreatesOwnLoggerForEveryBlock()
        {
            var factory = new RuntimeFunctionBlockFactory(GetAssemblies());
            var blocksExpectedToSucceed = CreateGoodFunctionBlocks().ToList();
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            using var loggerFactory = new TestLoggerFactory();

            var (instances, _) = factory.CreateInstances(blocksExpectedToSucceed, uniqueNameProvider, loggerFactory, Substitute.For<ILogger>());

            instances.Select(x => x.RuntimeBlock).Cast<IHasLogger>().Select(x => x.Logger).Should()
                .NotContainNulls().And
                .OnlyHaveUniqueItems();
        }

        [Fact]
        public void CreatesCorrectLoggerCategory()
        {
            var factory = new RuntimeFunctionBlockFactory(GetAssemblies());
            var blocksExpectedToSucceed = CreateGoodFunctionBlocks().ToList();
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            uniqueNameProvider.GetUniqueName(Arg.Any<IIdProvider>())
                .Returns(x => x[0] is IFunctionBlock block ? block.Name : string.Empty);
            using var loggerFactory = new TestLoggerFactory();

            var (instances, _) = factory.CreateInstances(blocksExpectedToSucceed, uniqueNameProvider, loggerFactory, Substitute.For<ILogger>());

            instances.Select(x => x.RuntimeBlock).Should()
                .AllSatisfy(x =>
                {
                    var loggerCategory = ((TestLogger)((IHasLogger)x).Logger).Category;
                    var expectedLoggerCategory = $"{uniqueNameProvider.GetUniqueName(x.FunctionBlock)} ({x.FunctionBlock.Id})";
                    loggerCategory.Should().Be(expectedLoggerCategory);
                });
        }

        private static IEnumerable<(IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors)> CreateGoodFunctionBlocks()
        {
            var runtimeBlock1Type = typeof(GoodDummyBlock1);
            var design1 = Substitute.For<IFunctionBlockDesign>();
            design1.Namespace.Returns(runtimeBlock1Type.Namespace);
            design1.Name.Returns(runtimeBlock1Type.Name);
            design1.RuntimeDriverTypeName.Returns((string?)null);
            design1.RuntimeTypeName.Returns((string?)null);
            var block1 = Substitute.For<IFunctionBlock>();
            block1.Id.Returns(Guid.NewGuid());
            block1.Design.Returns(design1);
            block1.Name.Returns(nameof(block1));
            AddFunctionBlockEnabledConnectors(block1);
            yield return (block1, null, []);

            var block2 = Substitute.For<IFunctionBlock>();
            block2.Id.Returns(Guid.NewGuid());
            block2.Design.Returns(design1);
            block2.Name.Returns(nameof(block2));
            AddFunctionBlockEnabledConnectors(block2);
            yield return (block2, null, []);

            var runtimeBlock3Type = typeof(GoodDummyBlock2);
            var driver3 = Substitute.For<IRuntimeDriver>();
            var design3 = Substitute.For<IFunctionBlockDesign>();
            design3.Namespace.Returns(runtimeBlock3Type.Namespace);
            design3.Name.Returns(runtimeBlock3Type.Name);
            design3.RuntimeTypeName.Returns((string?)null);
            var block3 = Substitute.For<IFunctionBlock>();
            block3.Id.Returns(Guid.NewGuid());
            block3.Design.Returns(design3);
            block3.Name.Returns(nameof(block3));
            AddFunctionBlockEnabledConnectors(block3);
            yield return (block3, driver3, []);

            var runtimeBlock4Type = typeof(GoodDummyBlock3);
            var design4 = Substitute.For<IFunctionBlockDesign>();
            design4.Namespace.Returns("Not");
            design4.Name.Returns("Existing");
            design4.RuntimeDriverTypeName.Returns((string?)null);
            design4.RuntimeTypeName.Returns(runtimeBlock4Type.FullName);
            var block4 = Substitute.For<IFunctionBlock>();
            block4.Id.Returns(Guid.NewGuid());
            block4.Design.Returns(design4);
            block4.Name.Returns(nameof(block4));
            AddFunctionBlockEnabledConnectors(block4);
            yield return (block4, null, []);

            static void AddFunctionBlockEnabledConnectors(IFunctionBlock block)
            {
                var functionBlockEnabledInput = Substitute.For<IConnector, IConnector<bool>, IPoolable>();
                functionBlockEnabledInput.Design.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
                block.SystemInputs.Returns([functionBlockEnabledInput]);
                var functionBlockEnabledOutput = Substitute.For<IConnector, IConnector<bool>>();
                functionBlockEnabledOutput.Design.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
                block.SystemOutputs.Returns([functionBlockEnabledOutput]);
            }
        }

        private static IEnumerable<(IFunctionBlock Block, IRuntimeDriver? Driver, HashSet<Guid> ConnectedConnectors)> CreateBadFunctionBlocks()
        {
            var design1 = Substitute.For<IFunctionBlockDesign>();
            design1.Namespace.Returns("Not");
            design1.Name.Returns("Existing");
            var block1 = Substitute.For<IFunctionBlock>();
            block1.Design.Returns(design1);
            yield return (block1, null, []);

            var runtimeBlock2Type = typeof(BadDummyBlock);
            var design2 = Substitute.For<IFunctionBlockDesign>();
            design2.Namespace.Returns(runtimeBlock2Type.Namespace);
            design2.Name.Returns(runtimeBlock2Type.Name);
            var block2 = Substitute.For<IFunctionBlock>();
            block2.Design.Returns(design2);
            yield return (block2, null, []);
        }
    }

    private static Assembly[] GetAssemblies()
        => [.. System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.Assemblies];
}

public abstract class DummyBlock<TBlock> : RuntimeFunctionBlock<TBlock>, IHasLogger where TBlock : RuntimeFunctionBlock<TBlock>, new()
{
    public new ILogger Logger => base.Logger;
}

namespace DummyBlocksA
{
    public class GoodDummyBlock1 : DummyBlock<GoodDummyBlock1> { }
}

namespace DummyBlocksB
{
    public class GoodDummyBlock2 : DummyBlock<GoodDummyBlock2> { }

    public class GoodDummyBlock3 : DummyBlock<GoodDummyBlock3> { }

    public class BadDummyBlock : DummyBlock<BadDummyBlock>, IRuntimeFunctionBlock
    {
        void IRuntimeFunctionBlock.Setup(IFunctionBlock functionBlock, IRuntimeDriver? driver, HashSet<Guid> connectedConnectors, IUniqueNameProvider uniqueNameProvider, ILogger logger)
            => throw new InvalidOperationException();
    }
}
