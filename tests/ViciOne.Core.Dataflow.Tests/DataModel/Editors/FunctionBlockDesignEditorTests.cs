using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class FunctionBlockDesignEditorTests
{
    protected PoolingFactory PoolingFactory { get; } = new PoolingFactory([Assembly.GetExecutingAssembly()]);

    public class Constructor : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void CreatesDesignIfNotSupplied()
        {
            var editor = new FunctionBlockDesignEditor(PoolingFactory);

            editor.Design.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfDesignIsCreated()
        {
            var editor = new FunctionBlockDesignEditor(PoolingFactory);

            editor.Design.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsDefaultCycleFrequencyIfDesignIsCreated()
        {
            var editor = new FunctionBlockDesignEditor(PoolingFactory);

            editor.Design.DefaultCycleFrequency.Should().Be(FunctionBlockDesignDefaults.CycleFrequency);
        }

        [Fact]
        public void SetsDefaultRunModeIfDesignIsCreated()
        {
            var editor = new FunctionBlockDesignEditor(PoolingFactory);

            editor.Design.DefaultRunMode.Should().Be(FunctionBlockDesignDefaults.RunMode);
        }

        [Fact]
        public void SetsValidRunModesIfDesignIsCreated()
        {
            var editor = new FunctionBlockDesignEditor(PoolingFactory);

            editor.Design.ValidRunModes.Should().ContainAllButNotMore(FunctionBlockDesignDefaults.ValidRunModes);
        }

        [Fact]
        public void CreatesEnabledInput()
        {
            var editor = new FunctionBlockDesignEditor(PoolingFactory);

            editor.Design.SystemInputs.Should()
                .Contain(x => x.Name == SystemConnectorNames.FunctionBlockEnabledName)
                .Which.DefaultValue.Should().BeOfType<bool>()
                .Which.Should().BeTrue();
        }

        [Fact]
        public void CreatesEnabledOutput()
        {
            var editor = new FunctionBlockDesignEditor(PoolingFactory);

            editor.Design.SystemOutputs.Should()
                .Contain(x => x.Name == SystemConnectorNames.FunctionBlockEnabledName)
                .Which.DefaultValue.Should().BeOfType<bool>()
                .Which.Should().BeFalse();
        }

        [Fact]
        public void StoresSuppliedDesign()
        {
            var design = new FunctionBlockDesign();

            var editor = new FunctionBlockDesignEditor(design);

            editor.Design.Should().BeSameAs(design);
        }
    }

    public class AddConnectorDesign : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void AddsConnectorDesign()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);

            blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            blockEditor.Design.ProcessDataInputs.Should().Contain(connectorEditor.Design);
        }

        [Fact]
        public void SetsEditor()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);

            blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            ((IConnectorDesignEditor)connectorEditor).FunctionBlockDesignEditor.Should().Be(blockEditor);
        }

        [Fact]
        public void DoesNothingIfAlreadyAdded()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);
            blockEditor.Design.ProcessDataInputs.Add(connectorEditor.Design);
            var countBefore = blockEditor.Design.ProcessDataInputs.Count;

            blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            blockEditor.Design.ProcessDataInputs.Should().HaveCount(countBefore);
        }

        [Fact]
        public void ThrowsIfAlreadAddedToAnotherBlock()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor1 = new MockFunctionBlockDesignEditor(PoolingFactory);
            var blockEditor2 = new MockFunctionBlockDesignEditor(PoolingFactory);
            blockEditor1.AddConnectorDesign(blockEditor1.Design.ProcessDataInputs, connectorEditor);

            Action action = () => blockEditor2.AddConnectorDesign(blockEditor2.Design.ProcessDataInputs, connectorEditor);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*added*");
        }

        [Fact]
        public void ThrowsIfNameAlreadyExisting()
        {
            var connectorEditor1 = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var connectorEditor2 = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor1);

            Action action = () => blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*existing*");
        }

        private sealed class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            public new void AddConnectorDesign(List<ConnectorDesign> designs, IConnectorDesignEditor editor)
                => base.AddConnectorDesign(designs, editor);
        }
    }

    public class AddPersistentVariable : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void AddsVariable()
        {
            var variableEditor = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            blockEditor.AddPersistentVariable(variableEditor);

            blockEditor.Design.PersistentVariables.Should().Contain(variableEditor.PersistentVariable);
        }

        [Fact]
        public void SetsEditor()
        {
            var variableEditor = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            blockEditor.AddPersistentVariable(variableEditor);

            ((IPersistentVariableDesignEditor)variableEditor).FunctionBlockDesignEditor.Should().Be(blockEditor);
        }

        [Fact]
        public void ThrowsIfAlreadAdded()
        {
            var variableEditor = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor1 = new FunctionBlockDesignEditor(PoolingFactory);
            var blockEditor2 = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor1.AddPersistentVariable(variableEditor);

            Action action = () => blockEditor2.AddPersistentVariable(variableEditor);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*added*");
        }

        [Fact]
        public void ThrowsIfNameAlreadyExisting()
        {
            var variableEditor1 = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var variableEditor2 = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddPersistentVariable(variableEditor1);

            Action action = () => blockEditor.AddPersistentVariable(variableEditor2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*existing*");
        }
    }

    public class AddProcessDataInputOutput : FunctionBlockDesignEditorTests
    {
        protected class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            public List<ConnectorDesign> PassedList { get; private set; } = [];

            protected override void AddConnectorDesign(List<ConnectorDesign> designs, IConnectorDesignEditor editor)
               => PassedList = designs;
        }
    }

    public class AddProcessDataInput : AddProcessDataInputOutput
    {
        [Fact]
        public void PassesCorrectConnectorDesignList()
        {
            var editor = new MockFunctionBlockDesignEditor(PoolingFactory);

            editor.AddProcessDataInput(Substitute.For<IConnectorDesignEditor>());

            editor.PassedList.Should().BeSameAs(editor.Design.ProcessDataInputs);
        }
    }

    public class AddProcessDataOutput : AddProcessDataInputOutput
    {
        [Fact]
        public void PassesCorrectConnectorDesignList()
        {
            var editor = new MockFunctionBlockDesignEditor(PoolingFactory);

            editor.AddProcessDataOutput(Substitute.For<IConnectorDesignEditor>());

            editor.PassedList.Should().BeSameAs(editor.Design.ProcessDataOutputs);
        }
    }

    public class AddSetting : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void AddsSetting()
        {
            var settingEditor = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            blockEditor.AddSetting(settingEditor);

            blockEditor.Design.Settings.Should().Contain(settingEditor.Setting);
        }

        [Fact]
        public void SetsEditor()
        {
            var settingEditor = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            blockEditor.AddSetting(settingEditor);

            ((ISettingDesignEditor)settingEditor).FunctionBlockDesignEditor.Should().Be(blockEditor);
        }

        [Fact]
        public void ThrowsIfAlreadAdded()
        {
            var settingEditor = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor1 = new FunctionBlockDesignEditor(PoolingFactory);
            var blockEditor2 = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor1.AddSetting(settingEditor);

            Action action = () => blockEditor2.AddSetting(settingEditor);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*added*");
        }

        [Fact]
        public void ThrowsIfNameAlreadyExisting()
        {
            var settingEditor1 = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var settingEditor2 = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddSetting(settingEditor1);

            Action action = () => blockEditor.AddSetting(settingEditor2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*existing*");
        }
    }

    public class AddValidRunMode : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void AddsRunMode()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.Design.ValidRunModes.Clear();

            blockEditor.AddValidRunMode(runMode);

            blockEditor.Design.ValidRunModes.Should().Contain(runMode);
        }

        [Fact]
        public void DoesNothingIfAlreadyAdded()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.Design.ValidRunModes.Clear();
            blockEditor.Design.ValidRunModes.Add(runMode);
            var countBefore = blockEditor.Design.ValidRunModes.Count;

            blockEditor.AddValidRunMode(runMode);

            blockEditor.Design.ValidRunModes.Should().HaveCount(countBefore);
        }
    }

    public class RemoveConnectorDesign : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void RemovesConnectorDesign()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            blockEditor.RemoveConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            blockEditor.Design.ProcessDataInputs.Should().NotContain(connectorEditor.Design);
        }

        [Fact]
        public void ResetsEditor()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            blockEditor.RemoveConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            ((IConnectorDesignEditor)connectorEditor).FunctionBlockDesignEditor.Should().BeNull();
        }

        [Fact]
        public void ReturnsTrueIfRemoved()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            var result = blockEditor.RemoveConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfNotRemoved()
        {
            var connectorEditor = new ConnectorDesignInputEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new MockFunctionBlockDesignEditor(PoolingFactory);

            var result = blockEditor.RemoveConnectorDesign(blockEditor.Design.ProcessDataInputs, connectorEditor);

            result.Should().BeFalse();
        }

        private sealed class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            public new void AddConnectorDesign(List<ConnectorDesign> designs, IConnectorDesignEditor editor)
                => base.AddConnectorDesign(designs, editor);

            public new bool RemoveConnectorDesign(List<ConnectorDesign> designs, IConnectorDesignEditor editor)
                => base.RemoveConnectorDesign(designs, editor);
        }
    }

    public class RemovePersistentVariable : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void RemovesVariable()
        {
            var variableEditor = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddPersistentVariable(variableEditor);

            blockEditor.RemovePersistentVariable(variableEditor);

            blockEditor.Design.PersistentVariables.Should().NotContain(variableEditor.PersistentVariable);
        }

        [Fact]
        public void ResetsEditor()
        {
            var variableEditor = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddPersistentVariable(variableEditor);

            blockEditor.RemovePersistentVariable(variableEditor);

            ((IPersistentVariableDesignEditor)variableEditor).FunctionBlockDesignEditor.Should().BeNull();
        }

        [Fact]
        public void ReturnsTrueIfRemoved()
        {
            var variableEditor = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddPersistentVariable(variableEditor);

            var result = blockEditor.RemovePersistentVariable(variableEditor);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfNotRemoved()
        {
            var variableEditor = new PersistentVariableDesignEditor<object>("zweiundvierzig");
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            var result = blockEditor.RemovePersistentVariable(variableEditor);

            result.Should().BeFalse();
        }
    }

    public class RemoveProcessDataInputOutput : FunctionBlockDesignEditorTests
    {
        protected class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            public const bool Result = true;

            public List<ConnectorDesign> PassedList { get; private set; } = [];

            protected override bool RemoveConnectorDesign(List<ConnectorDesign> designs, IConnectorDesignEditor editor)
            {
                PassedList = designs;
                return Result;
            }
        }
    }

    public class RemoveProcessDataInput : RemoveProcessDataInputOutput
    {
        [Fact]
        public void PassesCorrectConnectorDesignList()
        {
            var editor = new MockFunctionBlockDesignEditor(PoolingFactory);

            editor.RemoveProcessDataInput(Substitute.For<IConnectorDesignEditor>());

            editor.PassedList.Should().BeSameAs(editor.Design.ProcessDataInputs);
        }

        [Fact]
        public void ReturnsResult()
        {
            var editor = new MockFunctionBlockDesignEditor(PoolingFactory);

            var result = editor.RemoveProcessDataInput(Substitute.For<IConnectorDesignEditor>());

            result.Should().Be(MockFunctionBlockDesignEditor.Result);
        }
    }

    public class RemoveProcessDataOutput : RemoveProcessDataInputOutput
    {
        [Fact]
        public void PassesCorrectConnectorDesignList()
        {
            var editor = new MockFunctionBlockDesignEditor(PoolingFactory);

            editor.RemoveProcessDataOutput(Substitute.For<IConnectorDesignEditor>());

            editor.PassedList.Should().BeSameAs(editor.Design.ProcessDataOutputs);
        }

        [Fact]
        public void ReturnsResult()
        {
            var editor = new MockFunctionBlockDesignEditor(PoolingFactory);

            var result = editor.RemoveProcessDataOutput(Substitute.For<IConnectorDesignEditor>());

            result.Should().Be(MockFunctionBlockDesignEditor.Result);
        }
    }

    public class RemoveSetting : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void RemovesSetting()
        {
            var settingEditor = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddSetting(settingEditor);

            blockEditor.RemoveSetting(settingEditor);

            blockEditor.Design.Settings.Should().NotContain(settingEditor.Setting);
        }

        [Fact]
        public void ResetsEditor()
        {
            var settingEditor = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddSetting(settingEditor);

            blockEditor.RemoveSetting(settingEditor);

            ((ISettingDesignEditor)settingEditor).FunctionBlockDesignEditor.Should().BeNull();
        }

        [Fact]
        public void ReturnsTrueIfRemoved()
        {
            var settingEditor = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddSetting(settingEditor);

            var result = blockEditor.RemoveSetting(settingEditor);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfNotRemoved()
        {
            var settingEditor = new SettingDesignEditor<object>("zweiundvierzig", PoolingFactory);
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            var result = blockEditor.RemoveSetting(settingEditor);

            result.Should().BeFalse();
        }
    }

    public class RemoveValidRunMode : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void RemovesRunMode()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddValidRunMode(runMode);

            blockEditor.RemoveValidRunMode(runMode);

            blockEditor.Design.ValidRunModes.Should().NotContain(runMode);
        }

        [Fact]
        public void ReturnsTrueIfRemoved()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddValidRunMode(runMode);

            var result = blockEditor.RemoveValidRunMode(runMode);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfNotRemoved()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            var result = blockEditor.RemoveValidRunMode(runMode);

            result.Should().BeFalse();
        }

        [Fact]
        public void ThrowsIfDefaultRunModeShouldBeRemoved()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddValidRunMode(runMode);
            blockEditor.SetDefaultRunMode(runMode);

            Action action = () => blockEditor.RemoveValidRunMode(runMode);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*not*remove*default*");
        }

        [Fact]
        public void ThrowsIfLastRunModeShouldBeRemoved()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.Design.ValidRunModes.Clear();
            blockEditor.Design.ValidRunModes.Add(runMode);
            blockEditor.Design.DefaultRunMode = RunMode.Change;

            Action action = () => blockEditor.RemoveValidRunMode(runMode);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*one*must*remain*");
        }
    }

    public class SetDefaultCycleFrequency : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void SetsDefaultCycleFrequency()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            uint cycleFrequency = 42;

            blockEditor.SetDefaultCycleFrequency(cycleFrequency);

            blockEditor.Design.DefaultCycleFrequency.Should().Be(cycleFrequency);
        }
    }

    public class SetDefaultRunMode : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void SetsDefaultRunMode()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            blockEditor.AddValidRunMode(runMode);

            blockEditor.SetDefaultRunMode(runMode);

            blockEditor.Design.DefaultRunMode.Should().Be(runMode);
        }

        [Fact]
        public void ThrowsIfRunModeIsNotValid()
        {
            var runMode = RunMode.Cyclic;
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetDefaultRunMode(runMode);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*valid*");
        }
    }

    public class SetRuntimeDriverTypeName : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void SetsName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameToSet = "Zweiundvierzig";

            blockEditor.SetRuntimeDriverTypeName(nameToSet);

            blockEditor.Design.RuntimeDriverTypeName.Should().Be(nameToSet);
        }

        [Fact]
        public void AllowsSettingNull()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            blockEditor.SetRuntimeDriverTypeName(null);

            blockEditor.Design.RuntimeDriverTypeName.Should().BeNull();
        }

        [Fact]
        public void TrimsName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameToSet = "   Zweiundvierzig      ";
            var expectedName = "Zweiundvierzig";

            blockEditor.SetRuntimeDriverTypeName(nameToSet);

            blockEditor.Design.RuntimeDriverTypeName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNameIsEmptyOrWhiteSpace(string name)
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetRuntimeDriverTypeName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNameIsNotValidAssemblyName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetRuntimeDriverTypeName("Zwei/und/vierzig");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameBefore = "Zweiundvierzig";
            blockEditor.SetRuntimeDriverTypeName(nameBefore);

            try { blockEditor.SetRuntimeDriverTypeName("   "); }
            catch { }

            blockEditor.Design.RuntimeDriverTypeName.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsValidNamespaceOrClassNameValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameBefore = "Zweiundvierzig";
            blockEditor.SetRuntimeDriverTypeName(nameBefore);

            try { blockEditor.SetRuntimeDriverTypeName("Zwei/und/vierzig"); }
            catch { }

            blockEditor.Design.RuntimeDriverTypeName.Should().Be(nameBefore);
        }
    }

    public class SetRuntimeTypeName : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void SetsName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameToSet = "Zweiundvierzig";

            blockEditor.SetRuntimeTypeName(nameToSet);

            blockEditor.Design.RuntimeTypeName.Should().Be(nameToSet);
        }

        [Fact]
        public void AllowsSettingNull()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            blockEditor.SetRuntimeTypeName(null);

            blockEditor.Design.RuntimeTypeName.Should().BeNull();
        }

        [Fact]
        public void TrimsName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameToSet = "   Zweiundvierzig      ";
            var expectedName = "Zweiundvierzig";

            blockEditor.SetRuntimeTypeName(nameToSet);

            blockEditor.Design.RuntimeTypeName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNameIsEmptyOrWhiteSpace(string name)
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetRuntimeTypeName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNameIsNotValidAssemblyName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetRuntimeTypeName("Zwei/und/vierzig");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameBefore = "Zweiundvierzig";
            blockEditor.SetRuntimeTypeName(nameBefore);

            try { blockEditor.SetRuntimeTypeName("   "); }
            catch { }

            blockEditor.Design.RuntimeTypeName.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsValidNamespaceOrClassNameValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameBefore = "Zweiundvierzig";
            blockEditor.SetRuntimeTypeName(nameBefore);

            try { blockEditor.SetRuntimeTypeName("Zwei/und/vierzig"); }
            catch { }

            blockEditor.Design.RuntimeTypeName.Should().Be(nameBefore);
        }
    }

    public class SetName : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void SetsName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameToSet = "Zweiundvierzig";

            blockEditor.SetName(nameToSet);

            blockEditor.Design.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void TrimsName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameToSet = "   Zweiundvierzig      ";
            var expectedName = "Zweiundvierzig";

            blockEditor.SetName(nameToSet);

            blockEditor.Design.Name.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNameIsEmptyOrWhiteSpace(string name)
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNameIsNotValidNamespaceOrClassName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetName("Zwei und vierzig");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameBefore = "Zweiundvierzig";
            blockEditor.SetName(nameBefore);

            try { blockEditor.SetName("   "); }
            catch { }

            blockEditor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsValidNamespaceOrClassNameValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var nameBefore = "Zweiundvierzig";
            blockEditor.SetName(nameBefore);

            try { blockEditor.SetName("Zwei und vierzig"); }
            catch { }

            blockEditor.Design.Name.Should().Be(nameBefore);
        }
    }

    public class SetNamespace : FunctionBlockDesignEditorTests
    {
        [Fact]
        public void SetsNamespace()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var namespaceToSet = "Zweiundvierzig";

            blockEditor.SetNamespace(namespaceToSet);

            blockEditor.Design.Namespace.Should().Be(namespaceToSet);
        }

        [Fact]
        public void TrimsNamespace()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var namespaceToSet = "   Zweiundvierzig      ";
            var expectedNamespace = "Zweiundvierzig";

            blockEditor.SetNamespace(namespaceToSet);

            blockEditor.Design.Namespace.Should().Be(expectedNamespace);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNamespaceIsEmptyOrWhiteSpace(string namespaceToSet)
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetNamespace(namespaceToSet);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNamespaceIsNotValidNamespaceOrClassName()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);

            Action action = () => blockEditor.SetNamespace("Zwei und vierzig");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void DoesNotSetNamespaceIfNullOrWhiteSpaceValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var namespaceBefore = "Zweiundvierzig";
            blockEditor.SetNamespace(namespaceBefore);

            try { blockEditor.SetNamespace("   "); }
            catch { }

            blockEditor.Design.Namespace.Should().Be(namespaceBefore);
        }

        [Fact]
        public void DoesNotSetNamespaceIfIsValidNamespaceOrClassNameValidationFails()
        {
            var blockEditor = new FunctionBlockDesignEditor(PoolingFactory);
            var namespaceBefore = "Zweiundvierzig";
            blockEditor.SetNamespace(namespaceBefore);

            try { blockEditor.SetNamespace("Zwei und vierzig"); }
            catch { }

            blockEditor.Design.Namespace.Should().Be(namespaceBefore);
        }
    }

    public class ValidateConnectorDesignNameUnique
    {
        public static TheoryData<MockFunctionBlockDesignEditor, ConnectorDesign, IEnumerable<ConnectorDesign>> ArgumentsFor_PassesCorrectEnumerationToOverload
        {
            get
            {
                var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);

                var blockEditor = new MockFunctionBlockDesignEditor(poolingFactory);

                var expectedEnumeration1 = blockEditor.Design.SystemInputs;
                var connectorDesign1 = expectedEnumeration1.First();

                var expectedEnumeration2 = blockEditor.Design.SystemOutputs;
                var connectorDesign2 = expectedEnumeration2.First();

                var processDataConnectorEditorIn = new ConnectorDesignInputEditor<object>("in", poolingFactory);
                blockEditor.AddProcessDataInput(processDataConnectorEditorIn);
                var expectedEnumeration3 = blockEditor.Design.ProcessDataInputs;
                var connectorDesign3 = processDataConnectorEditorIn.Design;

                var processDataConnectorEditorOut = new ConnectorDesignOutputEditor<object>("out");
                blockEditor.AddProcessDataOutput(processDataConnectorEditorOut);
                var expectedEnumeration4 = blockEditor.Design.ProcessDataOutputs;
                var connectorDesign4 = processDataConnectorEditorOut.Design;

                return new()
                {
                    { blockEditor, connectorDesign1, expectedEnumeration1 },
                    { blockEditor, connectorDesign2, expectedEnumeration2 },
                    { blockEditor, connectorDesign3, expectedEnumeration3 },
                    { blockEditor, connectorDesign4, expectedEnumeration4 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_PassesCorrectEnumerationToOverload))]
        public void PassesCorrectEnumerationToOverload(MockFunctionBlockDesignEditor editor, ConnectorDesign design, IEnumerable<ConnectorDesign> expectedEnumeration)
        {
            editor.ValidateConnectorDesignNameNotExisting(design, string.Empty);

            editor.PassedEnumeration.Should().BeSameAs(expectedEnumeration);
        }

        public sealed class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            public IEnumerable<ConnectorDesign> PassedEnumeration { get; private set; } = [];

            protected override void ValidateConnectorDesignNameNotExisting(IEnumerable<ConnectorDesign> designs, string name)
                => PassedEnumeration = designs;
        }
    }
}
