using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class RuntimeFunctionBlockTests
{
    public class Changed
    {
        [Fact]
        public void GetsChangedFlagWithoutResettingIt()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();
            dummyBlock.MarkAsChanged();

            var changed = dummyBlock.Changed;

            changed.Should().BeTrue();
            dummyBlock.Changed.Should().BeTrue();
        }
    }

    public class Setup
    {
        private readonly ILogger _logger = Substitute.For<ILogger>();
        private readonly IUniqueNameProvider _uniqueNameProvider = Substitute.For<IUniqueNameProvider>();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanSetup(bool withDriver)
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock(
                withDriver: withDriver);
            var driver = withDriver ? Substitute.For<IRuntimeDriver>() : null;

            Action act = () => runtimeBlock.Setup(functionBlock, driver, [], _uniqueNameProvider, _logger);

            act.Should().NotThrow();
        }

        [Fact]
        public void ThrowsIfCalledAgain()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock();
            runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            Action act = () => runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not*allowed*");
        }

        [Fact]
        public void SystemInputsContainsFunctionBlockEnabled()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock();

            runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            ((IRuntimeFunctionBlock)runtimeBlock).SystemInputs.Should().Contain(x => x.Connector.Design.Name == SystemConnectorNames.FunctionBlockEnabledName);
        }

        [Fact]
        public void SystemOutputsContainsFunctionBlockEnabled()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock();

            runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            ((IRuntimeFunctionBlock)runtimeBlock).SystemOutputs.Should().Contain(x => x.Connector.Design.Name == SystemConnectorNames.FunctionBlockEnabledName);
        }

        [Fact]
        public void ThrowsIfDriverIsNullButShouldNotBeNull()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock(
                withDriver: true);

            Action act = () => runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().ContainEquivalentOf("driver");
        }

        [Fact]
        public void ThrowsIfEnabledInputIsMissing()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock(
                withFunctionBlockEnabledInput: false);

            Action act = () => runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*connector*not*found*");
        }

        [Fact]
        public void ThrowsIfEnabledOutputIsMissing()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock(
                withFunctionBlockEnabledOutput: false);

            Action act = () => runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*connector*not*found*");
        }

        [Fact]
        public void StoresFunctionBlock()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock();

            runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            runtimeBlock.FunctionBlock.Should().BeSameAs(functionBlock);
        }

        [Fact]
        public void StoresDriver()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock(
                withDriver: true);
            var driver = Substitute.For<IRuntimeDriver>();

            runtimeBlock.Setup(functionBlock, driver, [], _uniqueNameProvider, _logger);

            runtimeBlock.Driver.Should().BeSameAs(driver);
        }

        [Fact]
        public void StoresLogger()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock();

            runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            runtimeBlock.Logger.Should().BeSameAs(_logger);
        }

        [Fact]
        public void SetsCorrectRunMode()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock();

            runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            runtimeBlock.RunMode.Should().Be(functionBlock.RunMode);
        }

        [Fact]
        public void SetsCorrectCycleFrequency()
        {
            using var runtimeBlock = new TestObjects.DummyBlock();
            var functionBlock = TestObjects.CreateFunctionBlock();

            runtimeBlock.Setup(functionBlock, null, [], _uniqueNameProvider, _logger);

            runtimeBlock.CycleFrequency.Should().Be(functionBlock.CycleFrequency);
        }
    }

    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = dummyBlock.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var dummyBlock = TestObjects.CreateAndSetupDummyBlock();
            dummyBlock.Dispose();

            Action action = dummyBlock.Dispose;

            action.Should().NotThrow();
        }
    }

    public class FunctionBlockEnabled
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReturnsValueOfInputConnector(bool inputConnectorValue)
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(enabledInputValue: inputConnectorValue);

            dummyBlock.FunctionBlockEnabled.Should().Be(inputConnectorValue);
        }
    }

    public class GetAndResetChanged
    {
        [Fact]
        public void GetsChangedFlag()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();
            dummyBlock.MarkAsChanged();

            var changed = dummyBlock.GetAndResetChanged();

            changed.Should().BeTrue();
        }

        [Fact]
        public void ResetsChangedFlag()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();
            dummyBlock.MarkAsChanged();

            dummyBlock.GetAndResetChanged();

            dummyBlock.GetAndResetChanged().Should().BeFalse();
        }
    }

    public class GetUniqueName
    {
        [Fact]
        public void PassesArgumentToUniqueNameProvider()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();
            var idProvider = Substitute.For<IIdProvider>();
            var uniqueNameProvider = dummyBlock.UniqueNameProvider!;

            dummyBlock.GetUniqueName(idProvider);

            uniqueNameProvider.Received().GetUniqueName(idProvider);
        }

        [Fact]
        public void ReturnsResultFromUniqueNameProvider()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();
            var idProvider = Substitute.For<IIdProvider>();
            var uniqueNameProvider = dummyBlock.UniqueNameProvider!;
            var expectedResult = "UniqueName42";
            uniqueNameProvider.GetUniqueName(Arg.Any<IIdProvider>()).Returns(expectedResult);

            var result = dummyBlock.GetUniqueName(idProvider);

            result.Should().Be(expectedResult);
        }
    }

    public class CreateProcessDataInputConnector
    {
        [Fact]
        public void CanGetConnector()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            var connector = dummyBlock.CreateProcessDataInputConnector<double>(TestObjects.DummyBlock.ConnectorNames.DummyInput);

            connector.Should().BeAssignableTo<IRuntimeConnector<double>>();
        }

        [Fact]
        public void SetsConnectedFlagIfConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: true);

            var connector = dummyBlock.CreateProcessDataInputConnector<double>(TestObjects.DummyBlock.ConnectorNames.DummyInput);

            connector.Connected.Should().BeTrue();
        }

        [Fact]
        public void DoesNotSetConnectedFlagIfNotConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: false);

            var connector = dummyBlock.CreateProcessDataInputConnector<double>(TestObjects.DummyBlock.ConnectorNames.DummyInput);

            connector.Connected.Should().BeFalse();
        }

        [Fact]
        public void ThrowsIfTypeMismatch()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateProcessDataInputConnector<string>(TestObjects.DummyBlock.ConnectorNames.DummyInput);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*input*connector*not*found*");
        }

        [Fact]
        public void ThrowsIfNotFound()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateProcessDataInputConnector<double>("abc");

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*connector*not*found*");
        }
    }

    public class CreateProcessDataOutputConnector
    {
        [Fact]
        public void CanGetConnector()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            var connector = dummyBlock.CreateProcessDataOutputConnector<double>(TestObjects.DummyBlock.ConnectorNames.DummyOutput);

            connector.Should().BeAssignableTo<IRuntimeConnector<double>>();
        }

        [Fact]
        public void SetsConnectedFlagIfConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: true);

            var connector = dummyBlock.CreateProcessDataOutputConnector<double>(TestObjects.DummyBlock.ConnectorNames.DummyOutput);

            connector.Connected.Should().BeTrue();
        }

        [Fact]
        public void DoesNotSetConnectedFlagIfNotConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: false);

            var connector = dummyBlock.CreateProcessDataOutputConnector<double>(TestObjects.DummyBlock.ConnectorNames.DummyOutput);

            connector.Connected.Should().BeFalse();
        }

        [Fact]
        public void ThrowsIfTypeMismatch()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateProcessDataOutputConnector<string>(TestObjects.DummyBlock.ConnectorNames.DummyOutput);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*output*connector*not*found*");
        }

        [Fact]
        public void ThrowsIfNotFound()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateProcessDataOutputConnector<double>("abc");

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*connector*not*found*");
        }
    }

    public class CreatePersistentVariable
    {
        [Fact]
        public void CanGetPersistentVariable()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            var persistentVariable = dummyBlock.CreatePersistentVariable<int>(TestObjects.DummyBlock.PersistentVariableNames.DummyPersistentVariable);

            persistentVariable.Should().BeAssignableTo<IRuntimePersistentVariable<int>>();
        }

        [Fact]
        public void ThrowsIfTypeMismatch()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreatePersistentVariable<string>(TestObjects.DummyBlock.PersistentVariableNames.DummyPersistentVariable);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*persistent*variable*not*found*");
        }

        [Fact]
        public void ThrowsIfNotFound()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreatePersistentVariable<int>("abc");

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*persistent*variable*not*found*");
        }
    }

    public class CreateSetting
    {
        [Fact]
        public void CanGetSetting()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            var setting = dummyBlock.CreateSetting<int>(TestObjects.DummyBlock.SettingNames.DummySetting);

            setting.Should().BeAssignableTo<IRuntimeSetting<int>>();
        }

        [Fact]
        public void SetsConnectedFlagOfInputIfConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: true);

            var setting = dummyBlock.CreateSetting<int>(TestObjects.DummyBlock.SettingNames.DummySetting);

            setting.Input.Connected.Should().BeTrue();
        }

        [Fact]
        public void SetsConnectedFlagOfOutputIfConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: true);

            var setting = dummyBlock.CreateSetting<int>(TestObjects.DummyBlock.SettingNames.DummySetting);

            setting.Output.Connected.Should().BeTrue();
        }

        [Fact]
        public void DoesNotSetConnectedFlagOfInputIfNotConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: false);

            var setting = dummyBlock.CreateSetting<int>(TestObjects.DummyBlock.SettingNames.DummySetting);

            setting.Input.Connected.Should().BeFalse();
        }

        [Fact]
        public void DoesNotSetConnectedFlagOfOutputIfNotConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: false);

            var setting = dummyBlock.CreateSetting<int>(TestObjects.DummyBlock.SettingNames.DummySetting);

            setting.Output.Connected.Should().BeFalse();
        }

        [Fact]
        public void ThrowsIfTypeMismatch()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateSetting<string>(TestObjects.DummyBlock.SettingNames.DummySetting);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*setting*not*found*");
        }

        [Fact]
        public void ThrowsIfNotFound()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateSetting<int>("abc");

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*setting*not*found*");
        }
    }

    public class CreateSystemInputConnector
    {
        [Fact]
        public void CanGetConnector()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            var connector = dummyBlock.CreateSystemInputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);

            connector.Should().BeAssignableTo<IRuntimeConnector<bool>>();
        }

        [Fact]
        public void SetsConnectedFlagIfConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: true);

            var connector = dummyBlock.CreateSystemInputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);

            connector.Connected.Should().BeTrue();
        }

        [Fact]
        public void DoesNotSetConnectedFlagIfNotConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: false);

            var connector = dummyBlock.CreateSystemInputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);

            connector.Connected.Should().BeFalse();
        }

        [Fact]
        public void ThrowsIfTypeMismatch()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateSystemInputConnector<string>(SystemConnectorNames.FunctionBlockEnabledName);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*system*input*connector*not*found*");
        }

        [Fact]
        public void ThrowsIfNotFound()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateSystemInputConnector<double>("abc");

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*connector*not*found*");
        }
    }

    public class CreateSystemOutputConnector
    {
        [Fact]
        public void CanGetConnector()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            var connector = dummyBlock.CreateSystemOutputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);

            connector.Should().BeAssignableTo<IRuntimeConnector<bool>>();
        }

        [Fact]
        public void SetsConnectedFlagIfConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: true);

            var connector = dummyBlock.CreateSystemOutputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);

            connector.Connected.Should().BeTrue();
        }

        [Fact]
        public void DoesNotSetConnectedFlagIfNotConnected()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(setConnectorsConnected: false);

            var connector = dummyBlock.CreateSystemOutputConnector<bool>(SystemConnectorNames.FunctionBlockEnabledName);

            connector.Connected.Should().BeFalse();
        }

        [Fact]
        public void ThrowsIfTypeMismatch()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateSystemOutputConnector<string>(SystemConnectorNames.FunctionBlockEnabledName);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*system*output*connector*not*found*");
        }

        [Fact]
        public void ThrowsIfNotFound()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = () => dummyBlock.CreateSystemOutputConnector<double>("abc");

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*connector*not*found*");
        }
    }

    public class InitializeI
    {
        [Fact]
        public void CanInitialize()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            Action action = dummyBlock.InitializeI;

            action.Should().NotThrow();
        }

        [Fact]
        public void AutomaticallyInitializesInputsWhenDecoratedWithAttributes()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlockWithComponentAttributes();

            dummyBlock.InitializeI();

            dummyBlock.ProcessDataInputs.DummyInput.Should().NotBeNull();
        }

        [Fact]
        public void AutomaticallyInitializesOutputsWhenDecoratedWithAttributes()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlockWithComponentAttributes();

            dummyBlock.InitializeI();

            dummyBlock.ProcessDataOutputs.DummyOutput.Should().NotBeNull();
        }

        [Fact]
        public void AutomaticallyInitializesSettingsWhenDecoratedWithAttributes()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlockWithComponentAttributes();

            dummyBlock.InitializeI();

            dummyBlock.Settings.DummySetting.Should().NotBeNull();
        }

        [Fact]
        public void AutomaticallyInitializesPersistentVariablesWhenDecoratedWithAttributes()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlockWithComponentAttributes();

            dummyBlock.InitializeI();

            dummyBlock.PersistentVariables.DummyPersistentVariable.Should().NotBeNull();
        }

        [Fact]
        public void DoesNotThrowIfComponentAttributesMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupEmptyDummyBlock();

            Action action = dummyBlock.InitializeI;

            action.Should().NotThrow();
        }

        [Fact]
        public void DoesNotThrowIfPropertyForClassDecoratedWithComponentAttributeMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlockWithMissingPropertyForDecoratedComponent();

            Action action = dummyBlock.InitializeI;

            action.Should().NotThrow();
        }

        [Fact]
        public void ThrowsIfProcessDataInputConnectorIsMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                skipInitializeProcessDataInputs: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*inputs*");
        }

        [Fact]
        public void ThrowsIfProcessDataInputConnectorIsUnknown()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                addAdditionalProcessDataInput: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*inputs*");
        }

        [Fact]
        public void ThrowsIfProcessDataOutputConnectorIsMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                skipInitializeProcessDataOutputs: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*outputs*");
        }

        [Fact]
        public void ThrowsIfProcessDataOutputConnectorIsUnknown()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                addAdditionalProcessDataOutput: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*outputs*");
        }

        [Fact]
        public void ThrowsIfPersistentVariableIsMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                skipInitializePersistentVariables: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*persistent*variables*");
        }

        [Fact]
        public void ThrowsIfPersistentVariableIsUnknown()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                addAdditionalPersistentVariable: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*persistent*variables*");
        }

        [Fact]
        public void ThrowsIfSettingIsMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                skipInitializeSettings: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*settings*").And
                .Message.Should()
                    .NotContainAny(["inputs", "outputs"]);
        }

        [Fact]
        public void ThrowsIfSettingIsUnknown()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                addAdditionalSetting: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*settings*").And
                .Message.Should()
                    .NotContainAny(["inputs", "outputs"]);
        }

        [Fact]
        public void ThrowsIfSettingInputIsMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                addSettingsInputs: false);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*settings*inputs*");
        }

        [Fact]
        public void ThrowsIfSettingInputIsUnknown()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                addAdditionalSettingInput: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*settings*inputs*");
        }

        [Fact]
        public void ThrowsIfSettingOutputIsMissing()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                addSettingsOutputs: false);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*settings*outputs*");
        }

        [Fact]
        public void ThrowsIfSettingOutputIsUnknown()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock(
               addAdditionalSettingOutput: true);

            Action action = dummyBlock.InitializeI;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*differences*between*settings*outputs*");
        }
    }

    public class MarkAsChanged
    {
        [Fact]
        public void SetsChangedFlag()
        {
            using var dummyBlock = TestObjects.CreateAndSetupDummyBlock();

            dummyBlock.MarkAsChanged();

            dummyBlock.GetAndResetChanged().Should().BeTrue();
        }
    }

    public class ProcessCycle
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanProcess(bool enabledInputValue)
        {
            using IRuntimeFunctionBlock dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                enabledInputValue: enabledInputValue);
            var cycleInfo = Substitute.For<IFunctionBlockCycleInfo>();

            Action action = () => dummyBlock.ProcessCycle(cycleInfo);

            action.Should().NotThrow();
        }

        [Fact]
        public void SetsEnabledOutputIfEnabledInputWasChanged()
        {
            using IRuntimeFunctionBlock dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                enabledInputValue: false);
            var enabledInput = (IRuntimeConnector<bool>)dummyBlock.SystemInputs.First(x => string.Equals(x.Connector.Design.Name, SystemConnectorNames.FunctionBlockEnabledName, StringComparison.OrdinalIgnoreCase));
            var enabledOutput = (IRuntimeConnector<bool>)dummyBlock.SystemInputs.First(x => string.Equals(x.Connector.Design.Name, SystemConnectorNames.FunctionBlockEnabledName, StringComparison.OrdinalIgnoreCase));
            enabledInput.SetValueIfNotEqual(true);
            var cycleInfo = Substitute.For<IFunctionBlockCycleInfo>();

            dummyBlock.ProcessCycle(cycleInfo);

            enabledOutput.Value.Should().BeTrue();
            enabledOutput.Changed.Should().BeTrue();
        }

        [Fact]
        public void CallsOnFunctionBlockEnabledChangedIfEnabledInputWasChanged()
        {
            using IRuntimeFunctionBlock dummyBlock = TestObjects.CreateAndSetupDummyBlock(
                enabledInputValue: false);
            var enabledInput = (IRuntimeConnector<bool>)dummyBlock.SystemInputs.First(x => string.Equals(x.Connector.Design.Name, SystemConnectorNames.FunctionBlockEnabledName, StringComparison.OrdinalIgnoreCase));
            enabledInput.SetValueIfNotEqual(true);
            var cycleInfo = Substitute.For<IFunctionBlockCycleInfo>();

            dummyBlock.ProcessCycle(cycleInfo);

            ((TestObjects.DummyBlock)dummyBlock).OnFunctionBlockEnabledChangedCalled.Should().BeTrue();
        }

        [Fact]
        public void CallsVirtualProcessCycle()
        {
            using IRuntimeFunctionBlock dummyBlock = TestObjects.CreateAndSetupDummyBlock();
            var cycleInfo = Substitute.For<IFunctionBlockCycleInfo>();

            dummyBlock.ProcessCycle(cycleInfo);

            ((TestObjects.DummyBlock)dummyBlock).LastReceivedCycleInfoInProcessCycle.Should().BeSameAs(cycleInfo);
        }
    }

    public class SetConnectorValueIfNotEqual
    {
        [Theory]
        [InlineData(Validity.Valid)]
        [InlineData(Validity.Invalid)]
        public void CallsConnectorSetValidityIfNotEqual(Validity validity)
        {
            var connector = Substitute.For<IRuntimeConnector<object?>>();

            TestObjects.DummyBlock.SetConnectorValueIfNotEqual(connector, default, validity);

            connector.Received().SetValidityIfNotEqual(validity);
        }

        [Fact]
        public void CallsConnectorSetValueIfNotEqual()
        {
            var connector = Substitute.For<IRuntimeConnector<int>>();

            TestObjects.DummyBlock.SetConnectorValueIfNotEqual(connector, 42);

            connector.Received().SetValueIfNotEqual(42);
        }

        [Theory]
        [InlineData(Validity.Invalid, false, false)]
        [InlineData(Validity.Valid, false, true)]
        [InlineData(Validity.Invalid, true, true)]
        [InlineData(Validity.Valid, true, true)]
        public void ReturnsValueAccordingToResultsOfConnectorMethods(Validity setValidityResult, bool setValueResult, bool expectedReturnValue)
        {
            var connector = Substitute.For<IRuntimeConnector<object?>>();
            connector.SetValidityIfNotEqual(default).ReturnsForAnyArgs(setValidityResult != Validity.Invalid);
            connector.SetValueIfNotEqual(default).ReturnsForAnyArgs(setValueResult);

            var result = TestObjects.DummyBlock.SetConnectorValueIfNotEqual(connector, default);

            result.Should().Be(expectedReturnValue);
        }
    }

    public class SetPersistentVariableIfNotEqual
    {
        [Fact]
        public void CallsVariableSetValueIfNotEqual()
        {
            var variable = Substitute.For<IRuntimePersistentVariable<int>>();

            TestObjects.DummyBlock.SetPersistentVariableIfNotEqual(variable, 42);

            variable.Received().SetValueIfNotEqual(42);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReturnsResultOfVariableSetValueIfNotEqual(bool setValueResult)
        {
            var variable = Substitute.For<IRuntimePersistentVariable<object?>>();
            variable.SetValueIfNotEqual(default).ReturnsForAnyArgs(setValueResult);

            var result = TestObjects.DummyBlock.SetPersistentVariableIfNotEqual(variable, default);

            result.Should().Be(setValueResult);
        }
    }

    public class SetSettingValueIfNotEqual
    {
        [Fact]
        public void CallsSettingSetValueIfNotEqual()
        {
            var setting = Substitute.For<IRuntimeSetting<int>>();

            TestObjects.DummyBlock.SetSettingValueIfNotEqual(setting, 42);

            setting.Received().SetValueIfNotEqual(42);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReturnsResultOfSettingSetValueIfNotEqual(bool setValueResult)
        {
            var setting = Substitute.For<IRuntimeSetting<object?>>();
            setting.SetValueIfNotEqual(default).ReturnsForAnyArgs(setValueResult);

            var result = TestObjects.DummyBlock.SetSettingValueIfNotEqual(setting, default);

            result.Should().Be(setValueResult);
        }
    }

    private static class TestObjects
    {
        private static void CreateConnectors(IFunctionBlockDesign design)
        {
            var functionBlockEnabledInput = Substitute.For<IConnectorDesign<bool>>();
            functionBlockEnabledInput.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);

            var functionBlockEnabledOutput = Substitute.For<IConnectorDesign<bool>>();
            functionBlockEnabledOutput.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);

            design.SystemInputs.Returns([functionBlockEnabledInput]);
            design.SystemOutputs.Returns([functionBlockEnabledOutput]);

            var dummyInput = Substitute.For<IConnectorDesign<double>>();
            dummyInput.Name.Returns(DummyBlock.ConnectorNames.DummyInput);

            var dummyOutput = Substitute.For<IConnectorDesign<double>>();
            dummyOutput.Name.Returns(DummyBlock.ConnectorNames.DummyOutput);

            design.ProcessDataInputs.Returns([dummyInput]);
            design.ProcessDataOutputs.Returns([dummyOutput]);
        }

        private static void CreateConnectors(IFunctionBlock block,
            bool withFunctionBlockEnabledInput, bool withFunctionBlockEnabledOutput,
            bool enabledInputValue)
        {
            if (withFunctionBlockEnabledInput)
            {
                var functionBlockEnabledInput = Substitute.For<IConnector<bool>, IPoolable>();
                var functionBlockEnabledInputDesign = (IConnectorDesign<bool>)block.Design.SystemInputs.First(x => string.Equals(x.Name, SystemConnectorNames.FunctionBlockEnabledName, StringComparison.OrdinalIgnoreCase));
                functionBlockEnabledInput.Design.Returns(functionBlockEnabledInputDesign);
                ((IConnector)functionBlockEnabledInput).Design.Returns(functionBlockEnabledInputDesign);
                functionBlockEnabledInput.Id.Returns(Guid.NewGuid());
                functionBlockEnabledInput.Value = enabledInputValue;

                block.SystemInputs.Returns([functionBlockEnabledInput]);
            }
            else
                block.SystemInputs.Returns([]);

            if (withFunctionBlockEnabledOutput)
            {
                var functionBlockEnabledOutput = Substitute.For<IConnector<bool>>();
                var functionBlockEnabledOutputDesign = (IConnectorDesign<bool>)block.Design.SystemOutputs.First(x => string.Equals(x.Name, SystemConnectorNames.FunctionBlockEnabledName, StringComparison.OrdinalIgnoreCase));
                functionBlockEnabledOutput.Design.Returns(functionBlockEnabledOutputDesign);
                ((IConnector)functionBlockEnabledOutput).Design.Returns(functionBlockEnabledOutputDesign);
                functionBlockEnabledOutput.Id.Returns(Guid.NewGuid());

                block.SystemOutputs.Returns([functionBlockEnabledOutput]);
            }
            else
                block.SystemOutputs.Returns([]);

            var dummyInput = Substitute.For<IConnector<double>, IPoolable>();
            var dummyInputDesign = (IConnectorDesign<double>)block.Design.ProcessDataInputs.First(x => string.Equals(x.Name, DummyBlock.ConnectorNames.DummyInput, StringComparison.OrdinalIgnoreCase));
            dummyInput.Design.Returns(dummyInputDesign);
            ((IConnector)dummyInput).Design.Returns(dummyInputDesign);
            dummyInput.Id.Returns(Guid.NewGuid());

            block.ProcessDataInputs.Returns([dummyInput]);

            var dummyOutput = Substitute.For<IConnector<double>>();
            var dummyOutputDesign = (IConnectorDesign<double>)block.Design.ProcessDataOutputs.First(x => string.Equals(x.Name, DummyBlock.ConnectorNames.DummyOutput, StringComparison.OrdinalIgnoreCase));
            dummyOutput.Design.Returns(dummyOutputDesign);
            ((IConnector)dummyOutput).Design.Returns(dummyOutputDesign);
            dummyOutput.Id.Returns(Guid.NewGuid());

            block.ProcessDataOutputs.Returns([dummyOutput]);
        }

        public static DummyBlock CreateAndSetupDummyBlock(bool withDriver = false,
            bool skipInitializeProcessDataInputs = false, bool skipInitializeProcessDataOutputs = false,
            bool skipInitializeSettings = false, bool skipInitializePersistentVariables = false,
            bool addAdditionalProcessDataInput = false, bool addAdditionalProcessDataOutput = false,
            bool addAdditionalSetting = false, bool addAdditionalPersistentVariable = false,
            bool addSettingsInputs = true, bool addSettingsOutputs = true,
            bool addAdditionalSettingInput = false, bool addAdditionalSettingOutput = false,
            bool enabledInputValue = false, bool setConnectorsConnected = false)
        {
            var dummyBlock = new DummyBlock(
                skipInitializeProcessDataInputs, skipInitializeProcessDataOutputs,
                skipInitializeSettings, skipInitializePersistentVariables,
                addAdditionalProcessDataInput, addAdditionalProcessDataOutput,
                addAdditionalSetting, addAdditionalPersistentVariable);

            var block = CreateFunctionBlock(
                withDriver: withDriver,
                addSettingsInputs: addSettingsInputs,
                addSettingsOutputs: addSettingsOutputs,
                addAdditionalSettingInput: addAdditionalSettingInput,
                addAdditionalSettingOutput: addAdditionalSettingOutput,
                enabledInputValue: enabledInputValue);
            var driver = withDriver ? Substitute.For<IRuntimeDriver>() : null;
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            var logger = Substitute.For<ILogger>();

            var connectedConnectors = setConnectorsConnected
                ? block.GetConnectors().Select(x => x.Id).ToHashSet()
                : [];

            dummyBlock.UniqueNameProvider = uniqueNameProvider;
            dummyBlock.Setup(block, driver, connectedConnectors, uniqueNameProvider, logger);

            return dummyBlock;
        }

        public static DummyBlockWithComponentAttributes CreateAndSetupDummyBlockWithComponentAttributes()
            => CreateAndSetupDummyBlock<DummyBlockWithComponentAttributes>(CreateFunctionBlock());

        public static DummyBlockWithoutComponentAttributes CreateAndSetupEmptyDummyBlock()
           => CreateAndSetupDummyBlock<DummyBlockWithoutComponentAttributes>(CreateEmptyFunctionBlock());

        public static DummyBlockWithMissingPropertyForDecoratedComponent CreateAndSetupDummyBlockWithMissingPropertyForDecoratedComponent()
           => CreateAndSetupDummyBlock<DummyBlockWithMissingPropertyForDecoratedComponent>(CreateEmptyFunctionBlock());

        private static TBlock CreateAndSetupDummyBlock<TBlock>(IFunctionBlock block) where TBlock : RuntimeFunctionBlock<TBlock>, new()
        {
            var dummyBlock = new TBlock();
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            var logger = Substitute.For<ILogger>();

            dummyBlock.Setup(block, null, [], uniqueNameProvider, logger);

            return dummyBlock;
        }

        private static IFunctionBlockDesign CreateDesign(bool withDriver)
        {
            var design = Substitute.For<IFunctionBlockDesign>();
            if (withDriver)
                design.RuntimeDriverTypeName.Returns("Zweiungvierzig");
            else
                design.RuntimeDriverTypeName.Returns((string?)null);

            CreateSettings(design);
            CreatePersistentVariables(design);
            CreateConnectors(design);

            return design;
        }

        private static IFunctionBlock CreateEmptyFunctionBlock()
        {
            var design = Substitute.For<IFunctionBlockDesign>();
            design.RuntimeDriverTypeName.Returns((string?)null);
            var functionBlockEnabledInputDesign = Substitute.For<IConnectorDesign<bool>>();
            functionBlockEnabledInputDesign.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
            var functionBlockEnabledOutputDesign = Substitute.For<IConnectorDesign<bool>>();
            functionBlockEnabledOutputDesign.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
            design.SystemInputs.Returns([functionBlockEnabledInputDesign]);
            design.SystemOutputs.Returns([functionBlockEnabledOutputDesign]);

            var block = Substitute.For<IFunctionBlock>();
            block.Design.Returns(design);

            var functionBlockEnabledInput = Substitute.For<IConnector<bool>, IPoolable>();
            functionBlockEnabledInput.Design.Returns(functionBlockEnabledInputDesign);
            ((IConnector)functionBlockEnabledInput).Design.Returns(functionBlockEnabledInputDesign);
            functionBlockEnabledInput.Id.Returns(Guid.NewGuid());
            functionBlockEnabledInput.Value = true;
            block.SystemInputs.Returns([functionBlockEnabledInput]);

            var functionBlockEnabledOutput = Substitute.For<IConnector<bool>>();
            functionBlockEnabledOutput.Design.Returns(functionBlockEnabledOutputDesign);
            ((IConnector)functionBlockEnabledOutput).Design.Returns(functionBlockEnabledOutputDesign);
            functionBlockEnabledOutput.Id.Returns(Guid.NewGuid());
            block.SystemOutputs.Returns([functionBlockEnabledOutput]);

            return block;
        }

        public static IFunctionBlock CreateFunctionBlock(bool withDriver = false,
            bool withFunctionBlockEnabledInput = true, bool withFunctionBlockEnabledOutput = true,
            bool addSettingsInputs = true, bool addSettingsOutputs = true,
            bool addAdditionalSettingInput = false, bool addAdditionalSettingOutput = false,
            bool enabledInputValue = false)
        {
            var design = CreateDesign(withDriver);

            var block = Substitute.For<IFunctionBlock>();
            block.Design.Returns(design);
            block.CycleFrequency.Returns((uint)42);

            CreateSettings(block, addSettingsInputs, addSettingsOutputs, addAdditionalSettingInput, addAdditionalSettingOutput);
            CreatePersistentVariables(block);
            CreateConnectors(block, withFunctionBlockEnabledInput, withFunctionBlockEnabledOutput, enabledInputValue);

            return block;
        }

        private static void CreatePersistentVariables(IFunctionBlockDesign design)
        {
            var dummyPersistentVariable = Substitute.For<IPersistentVariableDesign<int>>();
            dummyPersistentVariable.Name.Returns(DummyBlock.PersistentVariableNames.DummyPersistentVariable);

            design.PersistentVariables.Returns([dummyPersistentVariable]);
        }

        private static void CreatePersistentVariables(IFunctionBlock block)
        {
            var dummyPersistentVariableDesign = (IPersistentVariableDesign<int>)block.Design.PersistentVariables.First(x => string.Equals(x.Name, DummyBlock.PersistentVariableNames.DummyPersistentVariable, StringComparison.OrdinalIgnoreCase));

            var dummyPersistentVariable = Substitute.For<IPersistentVariable<int>>();
            dummyPersistentVariable.Design.Returns(dummyPersistentVariableDesign);
            ((IPersistentVariable)dummyPersistentVariable).Design.Returns(dummyPersistentVariableDesign);
            dummyPersistentVariable.Id.Returns(Guid.NewGuid());

            block.PersistentVariables.Returns([dummyPersistentVariable]);
        }

        private static void CreateSettings(IFunctionBlockDesign design)
        {
            var dummySettingInput = Substitute.For<IConnectorDesign<int>>();
            dummySettingInput.Name.Returns(DummyBlock.SettingNames.DummySetting);

            var dummySettingOutput = Substitute.For<IConnectorDesign<int>>();
            dummySettingOutput.Name.Returns(DummyBlock.SettingNames.DummySetting);

            var dummySetting = Substitute.For<ISettingDesign<int>>();
            dummySetting.Name.Returns(DummyBlock.SettingNames.DummySetting);
            dummySetting.Input.Returns(dummySettingInput);
            dummySetting.Output.Returns(dummySettingOutput);

            design.Settings.Returns([dummySetting]);
        }

        private static void CreateSettings(IFunctionBlock block,
            bool addSettingsInputs, bool addSettingsOutputs,
            bool addAdditionalSettingInput, bool addAdditionalSettingOutput)
        {
            var dummySettingDesign = (ISettingDesign<int>)block.Design.Settings.First(x => string.Equals(x.Name, DummyBlock.SettingNames.DummySetting, StringComparison.OrdinalIgnoreCase));

            var dummySettingInput = Substitute.For<IConnector<int>>();
            var dummySettingInputDesign = dummySettingDesign.Input;
            dummySettingInput.Design.Returns(dummySettingInputDesign);
            ((IConnector)dummySettingInput).Design.Returns(dummySettingInputDesign);
            ((IConnector)dummySettingInput).Design.Returns(dummySettingInputDesign);
            dummySettingInput.Id.Returns(Guid.NewGuid());

            var dummySettingOutput = Substitute.For<IConnector<int>>();
            var dummySettingOutputDesign = dummySettingDesign.Output;
            dummySettingOutput.Design.Returns(dummySettingOutputDesign);
            ((IConnector)dummySettingOutput).Design.Returns(dummySettingOutputDesign);
            ((IConnector)dummySettingOutput).Design.Returns(dummySettingOutputDesign);
            dummySettingOutput.Id.Returns(Guid.NewGuid());

            var dummySetting = Substitute.For<ISetting<int>>();
            dummySetting.Design.Returns(dummySettingDesign);
            ((ISetting)dummySetting).Design.Returns(dummySettingDesign);
            dummySetting.Input.Returns(dummySettingInput);
            ((ISetting)dummySetting).Input.Returns(dummySettingInput);
            dummySetting.Output.Returns(dummySettingOutput);
            ((ISetting)dummySetting).Output.Returns(dummySettingOutput);

            block.Settings.Returns([dummySetting,]);

            if (addSettingsInputs)
            {
                var inputs = new List<IConnector>() { dummySettingInput };
                if (addAdditionalSettingInput)
                    inputs.Add(dummySettingInput);
                block.SettingsInputs.Returns(inputs);
            }
            else
                block.SettingsInputs.Returns([]);

            if (addSettingsOutputs)
            {
                var inputs = new List<IConnector>() { dummySettingOutput };
                if (addAdditionalSettingOutput)
                    inputs.Add(dummySettingOutput);
                block.SettingsOutputs.Returns(inputs);
            }
            else
                block.SettingsOutputs.Returns([]);
        }

        public sealed class DummyBlock(
            bool skipInitializeProcessDataInputs = false, bool skipInitializeProcessDataOutputs = false,
            bool skipInitializeSettings = false, bool skipInitializePersistentVariables = false,
            bool addAdditionalProcessDataInput = false, bool addAdditionalProcessDataOutput = false,
            bool addAdditionalSetting = false, bool addAdditionalPersistentVariable = false) : RuntimeFunctionBlock<DummyBlock>
        {
            private readonly bool _skipInitializeProcessDataInputs = skipInitializeProcessDataInputs;
            private readonly bool _skipInitializeProcessDataOutputs = skipInitializeProcessDataOutputs;
            private readonly bool _skipInitializePersistentVariables = skipInitializePersistentVariables;
            private readonly bool _skipInitializeSettings = skipInitializeSettings;
            private readonly bool _addAdditionalProcessDataInput = addAdditionalProcessDataInput;
            private readonly bool _addAdditionalProcessDataOutput = addAdditionalProcessDataOutput;
            private readonly bool _addAdditionalPersistentVariable = addAdditionalPersistentVariable;
            private readonly bool _addAdditionalSetting = addAdditionalSetting;

            public new bool Changed => base.Changed;
            public new ILogger Logger => base.Logger;
            public IUniqueNameProvider? UniqueNameProvider { get; set; }
            public bool OnFunctionBlockEnabledChangedCalled { get; private set; }
            public IFunctionBlockCycleInfo? LastReceivedCycleInfoInProcessCycle { get; private set; }

            public DummyBlock() : this(false) { }

            public new IRuntimePersistentVariable<T> CreatePersistentVariable<T>(string name)
                => base.CreatePersistentVariable<T>(name);

            public new IRuntimeConnector<T> CreateProcessDataInputConnector<T>(string name)
                => base.CreateProcessDataInputConnector<T>(name);

            public new IRuntimeConnector<T> CreateProcessDataOutputConnector<T>(string name)
                => base.CreateProcessDataOutputConnector<T>(name);

            public new IRuntimeSetting<T> CreateSetting<T>(string name)
                => base.CreateSetting<T>(name);

            public new IRuntimeConnector<T> CreateSystemInputConnector<T>(string name)
                => base.CreateSystemInputConnector<T>(name);

            public new IRuntimeConnector<T> CreateSystemOutputConnector<T>(string name)
                => base.CreateSystemOutputConnector<T>(name);

            public new string GetUniqueName(IIdProvider idProvider)
                => base.GetUniqueName(idProvider);

            protected override void InitializeConnectors()
            {
                if (_skipInitializeProcessDataInputs == false)
                {
                    ProcessDataInputs.Add(CreateProcessDataInputConnector<double>(ConnectorNames.DummyInput));
                    if (_addAdditionalProcessDataInput)
                        ProcessDataInputs.Add(ProcessDataInputs.First());
                }
                if (_skipInitializeProcessDataOutputs == false)
                {
                    ProcessDataOutputs.Add(CreateProcessDataOutputConnector<double>(ConnectorNames.DummyOutput));
                    if (_addAdditionalProcessDataOutput)
                        ProcessDataOutputs.Add(ProcessDataOutputs.First());
                }
            }

            protected override void InitializePersistentVariables()
            {
                if (_skipInitializePersistentVariables)
                    return;

                PersistentVariables.Add(CreatePersistentVariable<int>(PersistentVariableNames.DummyPersistentVariable));
                if (_addAdditionalPersistentVariable)
                    PersistentVariables.Add(PersistentVariables.First());
            }

            protected override void InitializeSettings()
            {
                if (_skipInitializeSettings)
                    return;

                Settings.Add(CreateSetting<int>(SettingNames.DummySetting));
                if (_addAdditionalSetting)
                    Settings.Add(Settings.First());
            }

            protected override void OnFunctionBlockEnabledChanged()
            {
                OnFunctionBlockEnabledChangedCalled = true;
                base.OnFunctionBlockEnabledChanged();
            }

            protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
            {
                LastReceivedCycleInfoInProcessCycle = cycleInfo;
                base.ProcessCycle(cycleInfo);
            }

            public static new bool SetConnectorValueIfNotEqual<T>(IRuntimeConnector<T> connector, T value, Validity validity = Validity.Valid)
                => RuntimeFunctionBlock<DummyBlock>.SetConnectorValueIfNotEqual(connector, value, validity);

            public static new bool SetPersistentVariableIfNotEqual<T>(IRuntimePersistentVariable<T> variable, T value)
                => RuntimeFunctionBlock<DummyBlock>.SetPersistentVariableIfNotEqual(variable, value);

            public static new bool SetSettingValueIfNotEqual<T>(IRuntimeSetting<T> setting, T value)
                => RuntimeFunctionBlock<DummyBlock>.SetSettingValueIfNotEqual(setting, value);

            public static class ConnectorNames
            {
                public static string DummyInput { get; } = nameof(DummyInput);
                public static string DummyOutput { get; } = nameof(DummyOutput);
            }

            public static class PersistentVariableNames
            {
                public static string DummyPersistentVariable { get; } = nameof(DummyPersistentVariable);
            }

            public static class SettingNames
            {
                public static string DummySetting { get; } = nameof(DummySetting);
            }
        }

        public sealed class DummyBlockWithComponentAttributes : RuntimeFunctionBlock<DummyBlockWithComponentAttributes>
        {
            public new PersistentVariables_ PersistentVariables { get; } = new PersistentVariables_();
            [PersistentVariableDesigns]
            public sealed class PersistentVariables_
            {
                [PersistentVariableDesign("fbf4dae6-dca3-49d8-bf52-a8b5349266a3")]
                public IRuntimePersistentVariable<int>? DummyPersistentVariable { get; internal set; }
            }

            public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
            [InputConnectorDesigns]
            public sealed class Inputs_
            {
                [InputConnectorDesign("f9b6a266-e2a4-48db-93c1-766325962b4c")]
                public IRuntimeConnector<double>? DummyInput { get; internal set; }
            }

            public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
            [OutputConnectorDesigns]
            public sealed class Outputs_
            {
                [OutputConnectorDesign("50a1f5b2-7b10-44fd-846f-559d524ed32c")]
                public IRuntimeConnector<double>? DummyOutput { get; internal set; }
            }

            public new Settings_ Settings { get; } = new Settings_();
            [SettingDesigns]
            public sealed class Settings_
            {
                [SettingDesign("ff7e5054-03e3-418e-b94f-3750d65818a9", "04ab9bb2-fd14-499b-b33e-c6e1c877eec4", "f2c24217-655e-43b5-b19c-ec95c10dfdd7")]
                public IRuntimeSetting<int>? DummySetting { get; internal set; }
            }
        }

        public sealed class DummyBlockWithoutComponentAttributes : RuntimeFunctionBlock<DummyBlockWithoutComponentAttributes> { }

        public sealed class DummyBlockWithMissingPropertyForDecoratedComponent : RuntimeFunctionBlock<DummyBlockWithMissingPropertyForDecoratedComponent>
        {
            [InputConnectorDesigns]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "Needed for testing the initialization logic.")]
            public sealed class Inputs_ { }
        }
    }
}
