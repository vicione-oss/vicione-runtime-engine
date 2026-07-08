using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeFunctionBlockProcessorTests
{
    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Justification = "Wird durch den Aufruf von Should().NotThrow() aufgerufen.")]
        public void CanDispose()
        {
            var runtimeFunctionBlocks = Substitute.For<IEnumerable<IRuntimeFunctionBlock>>();
            using var linkCache = new LinkCache([], Substitute.For<IEnumerable<ILink>>(), () => Substitute.For<IPoolingValidator>(), EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            var processor = new RuntimeFunctionBlockProcessor(runtimeFunctionBlocks, linkCache, nameCache, logger, Guid.Empty);

            Action action = processor.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var runtimeFunctionBlocks = Substitute.For<IEnumerable<IRuntimeFunctionBlock>>();
            using var linkCache = new LinkCache([], Substitute.For<IEnumerable<ILink>>(), () => Substitute.For<IPoolingValidator>(), EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            var processor = new RuntimeFunctionBlockProcessor(runtimeFunctionBlocks, linkCache, nameCache, logger, Guid.Empty);
            processor.Dispose();

            Action action = processor.Dispose;

            action.Should().NotThrow();
        }
    }

    public class ProcessBlocks
    {
        [Fact]
        public void IncreasesCycleNo()
        {
            var runtimeFunctionBlocks = Substitute.For<IEnumerable<IRuntimeFunctionBlock>>();
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache([], Substitute.For<IEnumerable<ILink>>(), () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(runtimeFunctionBlocks, linkCache, nameCache, logger, Guid.Empty);
            var cycleNoBefore = processor.CycleNo;
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            processor.CycleNo.Should().Be(cycleNoBefore + 1);
        }

        [Fact]
        public void PoolsInputLinkGroupWhenLinkIsChanged()
        {
            var input = Substitute.For<IConnector<int>, IConnector, IPoolable>();
            var runtimeInput = Substitute.For<IRuntimeConnector<int>>();
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector)runtimeInput).Connector.Returns(input);
            var block = Substitute.For<IFunctionBlock>();
            block.ProcessDataInputs.Returns([input]);
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            var externalIncomingLink1 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            externalIncomingLink1.Destination.Returns(input);
            externalIncomingLink1.TypeConversion.Returns((ITypeConversion?)null);
            var runtimeLink1 = Substitute.For<IRuntimeLink>();
            runtimeLink1.Link.Returns(externalIncomingLink1);
            runtimeLink1.Changed.Returns(true);
            var inputLinkGroup = Substitute.For<IIncomingInputLinkGroup>();
            inputLinkGroup.Connector.Returns(runtimeInput);
            inputLinkGroup.Links.Returns([runtimeLink1]);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [externalIncomingLink1], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity, createIncomingInputLinkGroup: (_, _, _) => inputLinkGroup);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            processor.ProcessBlocks(validator, null, interceptor);
            inputLinkGroup.ClearReceivedCalls();

            processor.ProcessBlocks(validator, null, interceptor);

            inputLinkGroup.Received().Pool(validator, interceptor);
        }

        [Fact]
        public void PoolsInputLinkGroupWhenNeedsFurtherProcessingIsTrue()
        {
            var input = Substitute.For<IConnector<int>, IConnector, IPoolable>();
            var runtimeInput = Substitute.For<IRuntimeConnector<int>>();
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector)runtimeInput).Connector.Returns(input);
            var block = Substitute.For<IFunctionBlock>();
            block.ProcessDataInputs.Returns([input]);
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            var externalIncomingLink1 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            externalIncomingLink1.Destination.Returns(input);
            externalIncomingLink1.TypeConversion.Returns((ITypeConversion?)null);
            var runtimeLink1 = Substitute.For<IRuntimeLink>();
            runtimeLink1.Link.Returns(externalIncomingLink1);
            runtimeLink1.Changed.Returns(false);
            var inputLinkGroup = Substitute.For<IIncomingInputLinkGroup>();
            inputLinkGroup.Connector.Returns(runtimeInput);
            inputLinkGroup.Links.Returns([runtimeLink1]);
            inputLinkGroup.NeedsFurtherProcessing.Returns(true);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [externalIncomingLink1], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity, createIncomingInputLinkGroup: (_, _, _) => inputLinkGroup);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            inputLinkGroup.Received().Pool(validator, interceptor);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Keine reservierten Ausnahmetypen auslösen", Justification = "Ausreichend für die hier zu testende Funktionalität")]
        public void CanHandlePoolingErrors()
        {
            var design = Substitute.For<IConnectorDesign<int>, IConnectorDesign>();
            var designName = "DummyDesign";
            design.Name.Returns(designName);
            var input = Substitute.For<IConnector<int>, IConnector, IPoolable>();
            var inputId = "dc79108b-9e79-463b-a7cf-dc68a306e43a";
            input.Id.Returns(new Guid(inputId));
            input.Design.Returns(design);
            ((IConnector)input).Design.Returns(design);
            var runtimeInput = Substitute.For<IRuntimeConnector<int>>();
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector)runtimeInput).Connector.Returns(input);
            var block = Substitute.For<IFunctionBlock>();
            var blockName = "DummyBlock";
            var blockId = "75b0d953-99c8-4527-89a6-b8c6eae84863";
            block.ProcessDataInputs.Returns([input]);
            block.Name.Returns(blockName);
            block.Id.Returns(new Guid(blockId));
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            var externalIncomingLink1 = Substitute.For<IExternalIncomingLink, IExternalIncomingLink<int, int, int>>();
            externalIncomingLink1.Destination.Returns(input);
            externalIncomingLink1.TypeConversion.Returns((ITypeConversion?)null);
            var runtimeLink1 = Substitute.For<IRuntimeLink>();
            runtimeLink1.Link.Returns(externalIncomingLink1);
            runtimeLink1.Changed.Returns(true);
            var inputLinkGroup = Substitute.For<IIncomingInputLinkGroup>();
            inputLinkGroup.Connector.Returns(runtimeInput);
            inputLinkGroup.Links.Returns([runtimeLink1]);
            inputLinkGroup
                .When(x => x.Pool(Arg.Any<IPoolingValidator>(), Arg.Any<IConnectorValueTransferInterceptor>()))
                .Throw(new Exception());
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [externalIncomingLink1], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity, createIncomingInputLinkGroup: (_, _, _) => inputLinkGroup);
            var dataflow = Substitute.For<IDataflow>();
            dataflow.RootContainer.FunctionBlocks.Returns([block]);
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var logger = new TestLogger();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should()
                .Contain(designName).And
                .Contain(inputId).And
                .Contain(blockName).And
                .Contain(blockId);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Keine reservierten Ausnahmetypen auslösen", Justification = "Ausreichend für die hier zu testende Funktionalität")]
        public void CanHandleOutgoingInputLinkGroupProcessingErrors()
        {
            var design = Substitute.For<IConnectorDesign<int>>();
            var designName = "DummyDesign";
            design.Name.Returns(designName);
            var input = Substitute.For<IConnector<int>, IConnector, IPoolable>();
            var inputId = "a4e68880-138b-4ec8-97af-e0eae9ee7926";
            input.Id.Returns(new Guid(inputId));
            ((IConnector)input).Design.Returns(design);
            var runtimeInput = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            runtimeInput.Connector.Returns(input);
            ((IRuntimeConnector)runtimeInput).Connector.Returns(input);
            runtimeInput.Changed.Returns(true);
            runtimeInput.GetAndResetChanged().Returns(true);
            var block = Substitute.For<IFunctionBlock>();
            var blockName = "DummyBlock";
            var blockId = "2789efa7-add6-410e-a2ba-5b5fdc2d1eac";
            block.ProcessDataInputs.Returns([input]);
            block.Name.Returns(blockName);
            block.Id.Returns(new Guid(blockId));
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput]);
            var externalOutgoingLink = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<int>>();
            externalOutgoingLink.Source.Returns(input);
            externalOutgoingLink.TypeConversion.Returns((ITypeConversion?)null);
            var runtimeLink = Substitute.For<IRuntimeLink>();
            runtimeLink.Link.Returns(externalOutgoingLink);
            runtimeLink
                .When(x => x.SetData(Arg.Any<object?>(), Arg.Any<DateTime>(), Arg.Any<Validity>()))
                .Do(x => { throw new Exception(); });
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [externalOutgoingLink], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity, createRuntimeLink: (_, _) => runtimeLink);
            var dataflow = Substitute.For<IDataflow>();
            dataflow.RootContainer.FunctionBlocks.Returns([block]);
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var logger = new TestLogger();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should()
                .Contain("transferring").And
                .Contain("input").And
                .Contain(designName).And
                .Contain(inputId).And
                .Contain(blockName).And
                .Contain(blockId);
        }

        [Fact]
        public void ProcessesSettingsOnFirstCycle()
        {
            var settingsInput = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            settingsInput.Changed.Returns(false);
            var setting = Substitute.For<IRuntimeSetting<int>>();
            setting.Input.Returns(settingsInput);
            ((IRuntimeSetting)setting).Input.Returns(settingsInput);
            var settings = Substitute.For<IRuntimeSettings>();
            List<IRuntimeSetting> list = [setting];
            settings.GetEnumerator().Returns(list.GetEnumerator());
            ((IEnumerable)settings).GetEnumerator().Returns(list.GetEnumerator());
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.Settings.Returns(settings);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            settings.ReceivedWithAnyArgs().Process(Arg.Any<IFunctionBlockCycleInfo>());
        }

        [Fact]
        public void ProcessesSettingsWhenInputIsChanged()
        {
            var settingsInput = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            settingsInput.Changed.Returns(true);
            var setting = Substitute.For<IRuntimeSetting<int>>();
            setting.Input.Returns(settingsInput);
            ((IRuntimeSetting)setting).Input.Returns(settingsInput);
            var settings = Substitute.For<IRuntimeSettings>();
            List<IRuntimeSetting> list = [setting];
            settings.GetEnumerator().Returns(_ => list.GetEnumerator());
            ((IEnumerable)settings).GetEnumerator().Returns(_ => list.GetEnumerator());
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock.Settings.Returns(settings);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            processor.ProcessBlocks(validator, null, interceptor);
            settings.ClearReceivedCalls();

            processor.ProcessBlocks(validator, null, interceptor);

            settings.ReceivedWithAnyArgs().Process(Arg.Any<IFunctionBlockCycleInfo>());
        }

        [Fact]
        public void ProcessesSettingsWhenSettingIsChanged()
        {
            var settingsInput = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            var setting = Substitute.For<IRuntimeSetting<int>>();
            setting.Changed.Returns(true);
            setting.Input.Returns(settingsInput);
            ((IRuntimeSetting)setting).Input.Returns(settingsInput);
            var settings = Substitute.For<IRuntimeSettings>();
            List<IRuntimeSetting> list = [setting];
            settings.GetEnumerator().Returns(_ => list.GetEnumerator());
            ((IEnumerable)settings).GetEnumerator().Returns(_ => list.GetEnumerator());
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock.Settings.Returns(settings);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            processor.ProcessBlocks(validator, null, interceptor);
            settings.ClearReceivedCalls();

            processor.ProcessBlocks(validator, null, interceptor);

            settings.ReceivedWithAnyArgs().Process(Arg.Any<IFunctionBlockCycleInfo>());
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Keine reservierten Ausnahmetypen auslösen", Justification = "Ausreichend für die hier zu testende Funktionalität")]
        public void CanHandleSettingsProcessingErrors()
        {
            var settingsInput = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            settingsInput.Changed.Returns(true);
            var setting = Substitute.For<IRuntimeSetting<int>>();
            setting.Input.Returns(settingsInput);
            ((IRuntimeSetting)setting).Input.Returns(settingsInput);
            var settings = Substitute.For<IRuntimeSettings>();
            settings
              .When(x => x.Process(Arg.Any<IFunctionBlockCycleInfo>()))
              .Throw(new Exception());
            List<IRuntimeSetting> list = [setting];
            settings.GetEnumerator().Returns(list.GetEnumerator());
            ((IEnumerable)settings).GetEnumerator().Returns(list.GetEnumerator());
            var block = Substitute.For<IFunctionBlock>();
            var blockName = "DummyBlock";
            var blockId = "46cf184c-2783-496f-8645-d7aa87ac2e49";
            block.Name.Returns(blockName);
            block.Id.Returns(new Guid(blockId));
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.Settings.Returns(settings);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var dataflow = Substitute.For<IDataflow>();
            dataflow.RootContainer.FunctionBlocks.Returns([block]);
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var logger = new TestLogger();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should()
                .Contain("settings").And
                .Contain(blockName).And
                .Contain(blockId);
        }

        [Fact]
        public void ProcessesBlocksAccordingToRunMode_Change()
        {
            var block1 = Substitute.For<IFunctionBlock>();
            var block2 = Substitute.For<IFunctionBlock>();
            var runtimeBlock1 = Substitute.For<IRuntimeFunctionBlock>();
            var runtimeBlock2 = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock1.FunctionBlock.Returns(block1);
            runtimeBlock2.FunctionBlock.Returns(block2);
            runtimeBlock1.RunMode.Returns(RunMode.Change);
            runtimeBlock2.RunMode.Returns(RunMode.Change);
            runtimeBlock1.GetAndResetChanged().Returns(false);
            runtimeBlock2.GetAndResetChanged().Returns(true);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock1, runtimeBlock2];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            // ersten Zyklus aus Prüfung ausklammern, da dort immer alle Blöcke verarbeitet werden
            processor.ProcessBlocks(validator, null, interceptor);
            runtimeBlock1.ClearReceivedCalls();
            runtimeBlock2.ClearReceivedCalls();

            processor.ProcessBlocks(validator, null, interceptor);

            runtimeBlock1.DidNotReceiveWithAnyArgs().ProcessCycle(Arg.Any<IFunctionBlockCycleInfo>());
            runtimeBlock2.ReceivedWithAnyArgs().ProcessCycle(Arg.Any<IFunctionBlockCycleInfo>());
        }

        [Fact]
        public void ProcessesBlocksAccordingToRunMode_Cyclic()
        {
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)2);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            // ersten Zyklus aus Prüfung ausklammern, da dort immer alle Blöcke verarbeitet werden
            processor.ProcessBlocks(validator, null, interceptor);
            runtimeBlock.ClearReceivedCalls();

            for (var i = 0; i < 10; i++)
                processor.ProcessBlocks(validator, null, interceptor);

            runtimeBlock.ReceivedWithAnyArgs(5).ProcessCycle(Arg.Any<IFunctionBlockCycleInfo>());
        }

        [Fact]
        public void ProcessesAllBlocksInFirstCycle()
        {
            var block1 = Substitute.For<IFunctionBlock>();
            var block2 = Substitute.For<IFunctionBlock>();
            var runtimeBlock1 = Substitute.For<IRuntimeFunctionBlock>();
            var runtimeBlock2 = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock1.FunctionBlock.Returns(block1);
            runtimeBlock2.FunctionBlock.Returns(block2);
            runtimeBlock1.RunMode.Returns(RunMode.Change);
            runtimeBlock2.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock1.GetAndResetChanged().Returns(false);
            runtimeBlock2.CycleFrequency.Returns((uint)1234);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock1, runtimeBlock2];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            runtimeBlock1.ReceivedWithAnyArgs().ProcessCycle(Arg.Any<IFunctionBlockCycleInfo>());
            runtimeBlock2.ReceivedWithAnyArgs().ProcessCycle(Arg.Any<IFunctionBlockCycleInfo>());
        }

        [Fact]
        public void CanHandleInvalidBlockRunMode()
        {
            var block = Substitute.For<IFunctionBlock>();
            var blockName = "DummyBlock";
            var blockId = "9a677ea2-5466-45d5-90fb-a0b71159c71c";
            block.Name.Returns(blockName);
            block.Id.Returns(new Guid(blockId));
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            var invalidRunModeValue = int.MaxValue;
            runtimeBlock.RunMode.Returns((RunMode)invalidRunModeValue);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var dataflow = Substitute.For<IDataflow>();
            dataflow.RootContainer.FunctionBlocks.Returns([block]);
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var logger = new TestLogger();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should()
                .Contain(blockName).And
                .Contain(blockId).And
                .Contain(nameof(RunMode)).And
                .Contain(invalidRunModeValue.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Keine reservierten Ausnahmetypen auslösen", Justification = "Ausreichend für die hier zu testende Funktionalität")]
        public void CanHandleBlockProcessingErrors()
        {
            var block = Substitute.For<IFunctionBlock>();
            var blockName = "DummyBlock";
            var blockId = "31928fa4-d32b-4bf5-b263-6a13a2995ac3";
            block.Name.Returns(blockName);
            block.Id.Returns(new Guid(blockId));
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock
                .When(x => x.ProcessCycle(Arg.Any<IFunctionBlockCycleInfo>()))
                .Throw(new Exception());
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var dataflow = Substitute.For<IDataflow>();
            dataflow.RootContainer.FunctionBlocks.Returns([block]);
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var logger = new TestLogger();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should()
                .Contain("processing").And
                .Contain(blockName).And
                .Contain(blockId);
        }

        [Fact]
        public void ResetsChangedFlagOfBlockWhenBlockWasProcessed()
        {
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Change);
            runtimeBlock.GetAndResetChanged().Returns(true);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            runtimeBlock.Received().GetAndResetChanged();
        }

        [Fact]
        public void ResetsInputChangedFlagsWhenBlockWasProcessed()
        {
            var input1 = Substitute.For<IConnector<int>, IPoolable>();
            input1.Id.Returns(Guid.NewGuid());
            var input2 = Substitute.For<IConnector<bool>, IPoolable>();
            input2.Id.Returns(Guid.NewGuid());
            var runtimeInput1 = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            var runtimeInput2 = Substitute.For<IRuntimeConnector<bool>, IRuntimeConnector>();
            runtimeInput1.Connector.Returns(input1);
            runtimeInput2.Connector.Returns(input2);
            ((IRuntimeConnector)runtimeInput1).Connector.Returns(input1);
            ((IRuntimeConnector)runtimeInput2).Connector.Returns(input2);
            runtimeInput1.Changed.Returns(true);
            runtimeInput2.Changed.Returns(true);
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataInputs.Returns([runtimeInput1, runtimeInput2]);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            runtimeInput1.Received().GetAndResetChanged();
            runtimeInput2.Received().GetAndResetChanged();
        }

        [Fact]
        public void CallsResetChangedFlagsOnSettingsWhenBlockWasProcessed()
        {
            var block = Substitute.For<IFunctionBlock>();
            var settings = Substitute.For<IRuntimeSettings>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock.Settings.Returns(settings);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            settings.Received().ResetChangedFlags();
        }

        [Fact]
        public void CallsTransferValueToLinksOnChangedOutputLinkGroupWhenBlockWasProcessed()
        {
            var output1 = Substitute.For<IConnector<int>, IConnector>();
            output1.Id.Returns(Guid.NewGuid());
            var runtimeOutput1 = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            runtimeOutput1.Connector.Returns(output1);
            ((IRuntimeConnector)runtimeOutput1).Connector.Returns(output1);
            runtimeOutput1.Value.Returns(42);
            runtimeOutput1.Changed.Returns(false);
            runtimeOutput1.GetAndResetChanged().ReturnsForAnyArgs(false);
            var output2 = Substitute.For<IConnector<double>, IConnector>();
            output2.Id.Returns(Guid.NewGuid());
            var runtimeOutput2 = Substitute.For<IRuntimeConnector<double>, IRuntimeConnector>();
            runtimeOutput2.Connector.Returns(output2);
            ((IRuntimeConnector)runtimeOutput2).Connector.Returns(output2);
            runtimeOutput2.Value.Returns(66.6);
            runtimeOutput2.Changed.Returns(true);
            runtimeOutput2.GetAndResetChanged().ReturnsForAnyArgs(true);
            var block = Substitute.For<IFunctionBlock>();
            block.ProcessDataOutputs.Returns([output1, output2]);
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataOutputs.Returns([runtimeOutput1, runtimeOutput2]);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            var externalOutgoingLink1 = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<int>>();
            externalOutgoingLink1.Source.Returns(output1);
            var externalOutgoingLink2 = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<double>>();
            externalOutgoingLink2.Source.Returns(output2);
            externalOutgoingLink2.TypeConversion.Returns((ITypeConversion?)null);
            var runtimeLink1 = Substitute.For<IRuntimeLink>();
            runtimeLink1.Link.Returns(externalOutgoingLink1);
            var runtimeLink2 = Substitute.For<IRuntimeLink>();
            runtimeLink2.Link.Returns(externalOutgoingLink2);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [externalOutgoingLink1, externalOutgoingLink2], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity, createRuntimeLink: (link, _) =>
            {
                if (link == externalOutgoingLink1) return runtimeLink1;
                if (link == externalOutgoingLink2) return runtimeLink2;
                throw new NotImplementedException();
            });
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            runtimeLink1.DidNotReceiveWithAnyArgs().SetData(default, default, default);
            runtimeLink2.Received().SetData(66.6, Arg.Any<DateTime>(), Arg.Any<Validity>());
        }

        [Fact]
        public void CallsTransferValueToLinksOnChangedOutputLinkGroupWhenBlockWasNotProcessed()
        {
            var output = Substitute.For<IConnector<double>, IConnector>();
            var runtimeOutput = Substitute.For<IRuntimeConnector<double>, IRuntimeConnector>();
            runtimeOutput.Connector.Returns(output);
            ((IRuntimeConnector)runtimeOutput).Connector.Returns(output);
            runtimeOutput.Value.Returns(66.6);
            runtimeOutput.Changed.Returns(true);
            runtimeOutput.GetAndResetChanged().ReturnsForAnyArgs(true);
            var block = Substitute.For<IFunctionBlock>();
            block.ProcessDataOutputs.Returns([output]);
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.ProcessDataOutputs.Returns([runtimeOutput]);
            runtimeBlock.RunMode.Returns(RunMode.Change);
            var externalOutgoingLink = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<double>>();
            externalOutgoingLink.Source.Returns(output);
            externalOutgoingLink.TypeConversion.Returns((ITypeConversion?)null);
            var runtimeLink = Substitute.For<IRuntimeLink>();
            runtimeLink.Link.Returns(externalOutgoingLink);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [externalOutgoingLink], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity, createRuntimeLink: (_, _) => runtimeLink);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();
            // ersten Zyklus aus Prüfung ausklammern, da dort immer alle Blöcke verarbeitet werden
            processor.ProcessBlocks(validator, null, interceptor);
            runtimeBlock.ClearReceivedCalls();
            runtimeLink.ClearReceivedCalls();

            processor.ProcessBlocks(validator, null, interceptor);

            runtimeBlock.DidNotReceiveWithAnyArgs().ProcessCycle(Arg.Any<IFunctionBlockCycleInfo>());
            runtimeLink.Received().SetData(66.6, Arg.Any<DateTime>(), Arg.Any<Validity>());
        }

        [Fact]
        public void ResetsOutputChangedFlagAfterProcessingOutputs()
        {
            var output = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            output.Changed.Returns(true);
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock.ProcessDataOutputs.Returns([output]);
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            output.Received().GetAndResetChanged();
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Keine reservierten Ausnahmetypen auslösen", Justification = "Ausreichend für die hier zu testende Funktionalität")]
        public void CanHandleOutputProcessingErrors()
        {
            var design = Substitute.For<IConnectorDesign<int>>();
            var designName = "DummyDesign";
            design.Name.Returns(designName);
            var output = Substitute.For<IConnector<int>, IConnector>();
            var outputId = "1161f0d4-a285-4dfd-8406-a274536bacaf";
            output.Id.Returns(new Guid(outputId));
            ((IConnector)output).Design.Returns(design);
            var runtimeOutput = Substitute.For<IRuntimeConnector<int>, IRuntimeConnector>();
            runtimeOutput.Connector.Returns(output);
            ((IRuntimeConnector)runtimeOutput).Connector.Returns(output);
            runtimeOutput.Changed.Returns(true);
            runtimeOutput.GetAndResetChanged().Returns(true);
            var block = Substitute.For<IFunctionBlock>();
            var blockName = "DummyBlock";
            var blockId = "8df210c6-d279-4e11-97d3-4f89f94968ec";
            block.ProcessDataOutputs.Returns([output]);
            block.Name.Returns(blockName);
            block.Id.Returns(new Guid(blockId));
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock.ProcessDataOutputs.Returns([runtimeOutput]);
            var externalOutgoingLink = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<int>>();
            externalOutgoingLink.Source.Returns(output);
            externalOutgoingLink.TypeConversion.Returns((ITypeConversion?)null);
            var runtimeLink = Substitute.For<IRuntimeLink>();
            runtimeLink.Link.Returns(externalOutgoingLink);
            runtimeLink
                .When(x => x.SetData(Arg.Any<object?>(), Arg.Any<DateTime>(), Arg.Any<Validity>()))
                .Do(x => { throw new Exception(); });
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [externalOutgoingLink], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity, createRuntimeLink: (_, _) => runtimeLink);
            var dataflow = Substitute.For<IDataflow>();
            dataflow.RootContainer.FunctionBlocks.Returns([block]);
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var logger = new TestLogger();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);

            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should()
                .Contain("transferring").And
                .Contain("output").And
                .Contain(designName).And
                .Contain(outputId).And
                .Contain(blockName).And
                .Contain(blockId);
        }

        [Fact]
        public void ResetsIsFirstCycleFlagWhenBlockWasProcessed()
        {
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            IFunctionBlockCycleInfo? cycleInfo = null;
            var isFirstCycleBeforeProcessing = false;
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);
            runtimeBlock.ProcessCycle(Arg.Do<IFunctionBlockCycleInfo>(x =>
            {
                cycleInfo = x;
                isFirstCycleBeforeProcessing = cycleInfo.IsFirstCycle;
            }));
            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);
            var isFirstCycleAfterProcessing = cycleInfo!.IsFirstCycle;

            isFirstCycleBeforeProcessing.Should().BeTrue();
            isFirstCycleAfterProcessing.Should().BeFalse();
        }

        [Fact]
        public void SetsLastProcessedCycleNoWhenBlockWasProcessed()
        {
            var block = Substitute.For<IFunctionBlock>();
            var runtimeBlock = Substitute.For<IRuntimeFunctionBlock>();
            runtimeBlock.FunctionBlock.Returns(block);
            runtimeBlock.RunMode.Returns(RunMode.Cyclic);
            runtimeBlock.CycleFrequency.Returns((uint)1);

            IRuntimeFunctionBlock[] blocks = [runtimeBlock];
            var validator = Substitute.For<IPoolingValidator>();
            using var linkCache = new LinkCache(blocks, [], () => validator, EngineQueueOptions.DefaultPerLinkQueueCapacity);
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var logger = Substitute.For<ILogger>();
            using var processor = new RuntimeFunctionBlockProcessor(blocks, linkCache, nameCache, logger, Guid.Empty);
            IFunctionBlockCycleInfo? cycleInfo = null;
            var cycleNoWhileProcessing = ulong.MaxValue;
            runtimeBlock.ProcessCycle(Arg.Do<IFunctionBlockCycleInfo>(x =>
            {
                cycleInfo = x;
                cycleNoWhileProcessing = processor.CycleNo;
            }));
            using var interceptor = new DefaultConnectorValueTransferInterceptor();

            processor.ProcessBlocks(validator, null, interceptor);
            processor.ProcessBlocks(validator, null, interceptor);
            var lastProcessedCycleNoAfterProcessing = cycleInfo!.LastProcessedCycleNo;

            lastProcessedCycleNoAfterProcessing.Should().Be(cycleNoWhileProcessing);
        }
    }
}
