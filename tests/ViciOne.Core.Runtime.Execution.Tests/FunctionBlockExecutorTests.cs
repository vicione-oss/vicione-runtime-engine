using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using FunctionBlockExecutorTestsBlocks;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Execution;
using Xunit;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable correctly", Justification = "The class that tests the method \"Dispose\" should be named \"Dispose\".")]
public abstract class FunctionBlockExecutorTests : IDisposable
{
    private FunctionBlockExecutor Executor { get; } = CreateExecutor();

    protected FunctionBlockExecutorTests(bool startEngine = true)
    {
        if (startEngine)
            Executor.StartEngine();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not relevant for these tests.")]
    void IDisposable.Dispose()
    {
        Executor.Dispose();
        GC.SuppressFinalize(this);
    }

    public class ChangeInputValue : FunctionBlockExecutorTests
    {
        [Fact]
        public void SetsValue()
        {
            var input = Executor.GetInputs().First();
            var newValue = 42;

            Executor.ChangeInputValue(input, newValue);
            Executor.ProcessCycle();

            var inputValues = Executor.GetInputValues();
            inputValues[input].Value.Should().Be(newValue);
        }

        [Fact]
        public void SetsChangedTime()
        {
            var input = Executor.GetInputs().First();
            var timeBefore = DateTime.UtcNow;

            Executor.ChangeInputValue(input, 42);
            Executor.ProcessCycle();

            var inputValues = Executor.GetInputValues();
            inputValues[input].Time.Should().BeOnOrAfter(timeBefore)
                .And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public void SetsValidityToValid()
        {
            var input = Executor.GetInputs().First();

            Executor.ChangeInputValue(input, 42);
            Executor.ProcessCycle();

            var inputValues = Executor.GetInputValues();
            inputValues[input].Validity.Should().Be(Validity.Valid);
        }
    }

    public class ChangePersistentVariableValue : FunctionBlockExecutorTests
    {
        [Fact]
        public void SetsValue()
        {
            var variable = Executor.GetPersistentVariables().First();
            var newValue = 123;

            Executor.ChangePersistentVariableValue(variable, newValue);
            Executor.ProcessCycle();

            var variableValues = Executor.GetPersistentVariableValues();
            variableValues[variable].Value.Should().Be(newValue);
        }

        [Fact]
        public void SetsChangedTime()
        {
            var variable = Executor.GetPersistentVariables().First();
            var timeBefore = DateTime.UtcNow;

            Executor.ChangePersistentVariableValue(variable, 123);
            Executor.ProcessCycle();

            var variableValues = Executor.GetPersistentVariableValues();
            variableValues[variable].Time.Should().BeOnOrAfter(timeBefore)
                .And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public void SetsValidityToValid()
        {
            var variable = Executor.GetPersistentVariables().First();

            Executor.ChangePersistentVariableValue(variable, 123);
            Executor.ProcessCycle();

            var variableValues = Executor.GetPersistentVariableValues();
            variableValues[variable].Validity.Should().Be(Validity.Valid);
        }
    }

    public class ChangeSettingValue : FunctionBlockExecutorTests
    {
        [Fact]
        public void SetsValue()
        {
            var setting = Executor.GetSettings().First();
            var newValue = 456;

            Executor.ChangeSettingValue(setting, newValue);
            Executor.ProcessCycle();

            var settingValues = Executor.GetSettingValues();
            settingValues[setting].Value.Should().Be(newValue);
        }

        [Fact]
        public void SetsChangedTime()
        {
            var setting = Executor.GetSettings().First();
            var timeBefore = DateTime.UtcNow;

            Executor.ChangeSettingValue(setting, 456);
            Executor.ProcessCycle();

            var settingValues = Executor.GetSettingValues();
            settingValues[setting].Time.Should().BeOnOrAfter(timeBefore)
                .And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public void SetsValidityToValid()
        {
            var setting = Executor.GetSettings().First();

            Executor.ChangeSettingValue(setting, 456);
            Executor.ProcessCycle();

            var settingValues = Executor.GetSettingValues();
            settingValues[setting].Validity.Should().Be(Validity.Valid);
        }
    }

    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var executor = CreateExecutor();

            Action action = executor.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var executor = CreateExecutor();
            executor.Dispose();

            Action action = executor.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void StopsEngine()
        {
            var executor = CreateExecutor();
            executor.StartEngine();

            executor.Dispose();

            executor.EngineState.Should().Be(EngineState.Stopped);
        }
    }

    public class GetInputs : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsInputs()
        {
            var inputs = Executor.GetInputs();

            inputs.Select(x => x.Design.Name).Should().BeEquivalentTo(
                [
                    SystemConnectorNames.FunctionBlockEnabledName,
                    TestRuntimeFunctionBlock.TestSettingName,
                    TestRuntimeFunctionBlock.TestInputName,
                ]);
        }
    }

    public class GetInputValues : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsInputValues()
        {
            var input1 = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestInputName);
            var input2 = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestSettingName);
            var input3 = Executor.GetInputs().First(x => x.Design.Name == SystemConnectorNames.FunctionBlockEnabledName);
            var value1 = 42;
            var value2 = 123;
            var value3 = false;
            Executor.ChangeInputValue(input1, value1);
            Executor.ChangeInputValue(input2, value2);
            Executor.ChangeInputValue(input3, value3);
            Executor.ProcessCycle();

            var inputValues = Executor.GetInputValues();

            inputValues.Should().BeEquivalentTo(new Dictionary<IConnector, ChangedEntry>
            {
                [input1] = new ChangedEntry() { Value = value1, Validity = Validity.Valid },
                [input2] = new ChangedEntry() { Value = value2, Validity = Validity.Valid },
                [input3] = new ChangedEntry() { Value = value3, Validity = Validity.Valid },
            }, o => o.ExcludingMembersNamed(nameof(ChangedEntry.Id), nameof(ChangedEntry.Time)));
        }
    }

    public class GetOutputs : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsOutputs()
        {
            var outputs = Executor.GetOutputs();

            outputs.Select(x => x.Design.Name).Should().BeEquivalentTo(
                [
                    SystemConnectorNames.FunctionBlockEnabledName,
                    TestRuntimeFunctionBlock.TestSettingName,
                    TestRuntimeFunctionBlock.TestOutputName,
                ]);
        }
    }

    public class GetOutputValues : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsOutputValues()
        {
            var input1 = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestInputName);
            var input2 = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestSettingName);
            var input3 = Executor.GetInputs().First(x => x.Design.Name == SystemConnectorNames.FunctionBlockEnabledName);
            var output1 = Executor.GetOutputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestOutputName);
            var output2 = Executor.GetOutputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestSettingName);
            var output3 = Executor.GetOutputs().First(x => x.Design.Name == SystemConnectorNames.FunctionBlockEnabledName);
            var value1 = 42;
            var value2 = 123;
            var value3 = false;
            Executor.ChangeInputValue(input1, value1);
            Executor.ChangeInputValue(input2, value2);
            Executor.ChangeInputValue(input3, value3);
            Executor.ProcessCycle();

            var outputValues = Executor.GetOutputValues();

            outputValues.Should().BeEquivalentTo(new Dictionary<IConnector, ChangedEntry>
            {
                [output1] = new ChangedEntry() { Value = value1 * 2, Validity = Validity.Valid },
                [output2] = new ChangedEntry() { Value = value2, Validity = Validity.Valid },
                [output3] = new ChangedEntry() { Value = value3, Validity = Validity.Valid },
            }, o => o.ExcludingMembersNamed(nameof(ChangedEntry.Id), nameof(ChangedEntry.Time)));
        }
    }

    public class GetPersistentVariables : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsPersistentVariables()
        {
            var variables = Executor.GetPersistentVariables();

            variables.Select(x => x.Design.Name).Should().BeEquivalentTo(
                [
                    TestRuntimeFunctionBlock.TestVariableName,
                ]);
        }
    }

    public class GetPersistentVariableValues : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsPersistentVariableValues()
        {
            var input = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestInputName);
            var variable = Executor.GetPersistentVariables().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestVariableName);
            var value = 42;
            Executor.ChangeInputValue(input, value);
            Executor.ProcessCycle();

            var variableValues = Executor.GetPersistentVariableValues();

            variableValues.Should().BeEquivalentTo(new Dictionary<IPersistentVariable, ChangedEntry>
            {
                [variable] = new ChangedEntry() { Value = value * 3, Validity = Validity.Valid },
            }, o => o.ExcludingMembersNamed(nameof(ChangedEntry.Id), nameof(ChangedEntry.Time)));
        }
    }

    public class GetSettings : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsSettings()
        {
            var settings = Executor.GetSettings();

            settings.Select(x => x.Design.Name).Should().BeEquivalentTo(
                [
                    TestRuntimeFunctionBlock.TestSettingName,
                ]);
        }
    }

    public class GetSettingValues : FunctionBlockExecutorTests
    {
        [Fact]
        public void ReturnsSettingValues()
        {
            var input = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestSettingName);
            var setting = Executor.GetSettings().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestSettingName);
            var value = 42;
            Executor.ChangeInputValue(input, value);
            Executor.ProcessCycle();

            var settingValues = Executor.GetSettingValues();

            settingValues.Should().BeEquivalentTo(new Dictionary<ISetting, ChangedEntry>
            {
                [setting] = new ChangedEntry() { Value = value, Validity = Validity.Valid },
            }, o => o.ExcludingMembersNamed(nameof(ChangedEntry.Id), nameof(ChangedEntry.Time)));
        }
    }

    public class ProcessCycle : FunctionBlockExecutorTests
    {
        [Fact]
        public void PublishesCycleNo()
        {
            Executor.ProcessCycle();
            var cycleNoAfterFirstCycle = Executor.CycleNo;

            Executor.ProcessCycle();

            Executor.CycleNo.Should().Be(cycleNoAfterFirstCycle + 1);
        }

        [Fact]
        public void RaisesCycleStarted()
        {
            using var monitoredExecutor = Executor.Monitor();

            Executor.ProcessCycle();

            monitoredExecutor.Should()
                .Raise(nameof(FunctionBlockExecutor.CycleStarted))
                .WithSender(Executor)
                .WithArgs<ICycleInfo>(args => args.CycleNo == Executor.CycleNo);
        }

        [Fact]
        public void RaisesCycleFinished()
        {
            using var monitoredExecutor = Executor.Monitor();

            Executor.ProcessCycle();

            monitoredExecutor.Should()
                .Raise(nameof(FunctionBlockExecutor.CycleFinished))
                .WithSender(Executor)
                .WithArgs<ICycleInfo>(args => args.CycleNo == Executor.CycleNo);
        }

        [Fact]
        public void RaisesSettingValuesChangedIfTheyWereChanged()
        {
            var input = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestSettingName);
            Executor.ChangeInputValue(input, 42);
            using var monitoredExecutor = Executor.Monitor();

            Executor.ProcessCycle();

            monitoredExecutor.Should()
                .Raise(nameof(FunctionBlockExecutor.SettingValuesChanged))
                .WithSender(Executor);
        }

        [Fact]
        public void RaisesPersistentVariableValuesChangedIfTheyWereChanged()
        {
            // business logic in sample block changes the variable if the input changes
            var input = Executor.GetInputs().First(x => x.Design.Name == TestRuntimeFunctionBlock.TestInputName);
            Executor.ChangeInputValue(input, 42);
            using var monitoredExecutor = Executor.Monitor();

            Executor.ProcessCycle();

            monitoredExecutor.Should()
                .Raise(nameof(FunctionBlockExecutor.PersistentVariableValuesChanged))
                .WithSender(Executor);
        }

        [Fact]
        public void DoesNotRaiseSettingValuesChangedIfTheyWereNotChanged()
        {
            using var monitoredExecutor = Executor.Monitor();

            Executor.ProcessCycle();

            monitoredExecutor.Should()
                .NotRaise(nameof(FunctionBlockExecutor.SettingValuesChanged));
        }

        [Fact]
        public void DoesNotRaisePersistentVariableValuesChangedIfTheyWereNotChanged()
        {
            using var monitoredExecutor = Executor.Monitor();

            Executor.ProcessCycle();

            monitoredExecutor.Should()
                .NotRaise(nameof(FunctionBlockExecutor.PersistentVariableValuesChanged));
        }
    }

    public class StartEngine() : FunctionBlockExecutorTests(false)
    {
        [Fact]
        public void StartsEngine()
        {
            Executor.StartEngine();

            Executor.EngineState.Should().Be(EngineState.Starting);
        }

        [Fact]
        public void RaisesEngineStateChanged()
        {
            using var monitoredExecutor = Executor.Monitor();

            Executor.StartEngine();

            monitoredExecutor.Should()
                .Raise(nameof(FunctionBlockExecutor.EngineStateChanged))
                .WithSender(Executor);
        }
    }

    public class StopEngine : FunctionBlockExecutorTests
    {
        [Fact]
        public void StopsEngine()
        {
            Executor.StopEngine();

            Executor.EngineState.Should().Be(EngineState.Stopped);
        }

        [Fact]
        public void RaisesEngineStateChanged()
        {
            using var monitoredExecutor = Executor.Monitor();

            Executor.StopEngine();

            monitoredExecutor.Should()
                .Raise(nameof(FunctionBlockExecutor.EngineStateChanged))
                .WithSender(Executor);
        }
    }

    private static FunctionBlockExecutor CreateExecutor()
    {
        var design = CreateTestFunctionBlockDesign();
        var dataflowInfo = DataflowGenerator.GenerateDataflow([design]);
        return new FunctionBlockExecutor(dataflowInfo, [typeof(FunctionBlockExecutorTests).Assembly], NullLoggerFactory.Instance);
    }

    private static FunctionBlockDesign CreateTestFunctionBlockDesign()
    {
        PoolingFactory poolingFactory = new([]);
        var editor = new FunctionBlockDesignEditor(poolingFactory);
        editor.SetNamespace(typeof(TestRuntimeFunctionBlock).Namespace!);
        editor.SetName(nameof(TestRuntimeFunctionBlock));
        editor.SetRuntimeTypeName(typeof(TestRuntimeFunctionBlock).FullName);
        editor.AddProcessDataInput(new ConnectorDesignInputEditor<int>(TestRuntimeFunctionBlock.TestInputName, poolingFactory));
        editor.AddProcessDataOutput(new ConnectorDesignOutputEditor<int>(TestRuntimeFunctionBlock.TestOutputName));
        editor.AddSetting(new SettingDesignEditor<int>(TestRuntimeFunctionBlock.TestSettingName, poolingFactory));
        editor.AddPersistentVariable(new PersistentVariableDesignEditor<int>(TestRuntimeFunctionBlock.TestVariableName));
        return editor.Design;
    }
}

namespace FunctionBlockExecutorTestsBlocks
{
    public class TestRuntimeFunctionBlock : RuntimeFunctionBlock<TestRuntimeFunctionBlock>
    {
        internal const string TestInputName = "TestInput";
        internal const string TestOutputName = "TestOutput";
        internal const string TestSettingName = "TestSetting";
        internal const string TestVariableName = "TestVariable";

        private IRuntimeConnector<int>? _testInput;
        private IRuntimeConnector<int>? _testOutput;
        private IRuntimePersistentVariable<int>? _testVariable;

        protected override void InitializeConnectors()
        {
            ProcessDataInputs.Add(_testInput = CreateProcessDataInputConnector<int>(TestInputName));
            ProcessDataOutputs.Add(_testOutput = CreateProcessDataOutputConnector<int>(TestOutputName));
        }

        protected override void InitializeSettings()
            => Settings.Add(CreateSetting<int>(TestSettingName));

        protected override void InitializePersistentVariables()
            => PersistentVariables.Add(_testVariable = CreatePersistentVariable<int>(TestVariableName));

        protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
        {
            base.ProcessCycle(cycleInfo);
            if (_testInput is not null && _testInput.Changed)
            {
                if (_testOutput is not null)
                    SetConnectorValueIfNotEqual(_testOutput, _testInput.Value * 2);
                if (_testVariable is not null)
                    SetPersistentVariableIfNotEqual(_testVariable, _testInput.Value * 3);
            }
        }
    }
}
