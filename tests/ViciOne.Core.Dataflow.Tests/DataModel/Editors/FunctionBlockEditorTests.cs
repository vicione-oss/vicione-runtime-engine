using AwesomeAssertions;
using System;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class FunctionBlockEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesBlockIfNotSupplied()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.Should().NotBeNull();
            editor.FunctionBlock.Design.Should().BeSameAs(design);
        }

        [Fact]
        public void SetsNewIdIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsNameIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.Name.Should().Be(design.Name);
        }

        [Fact]
        public void SetsCycleFrequencyIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.CycleFrequency.Should().Be(design.DefaultCycleFrequency);
        }

        [Fact]
        public void SetsRunModeIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.RunMode.Should().Be(design.DefaultRunMode);
        }

        [Fact]
        public void CreatesPersistentVariablesIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.PersistentVariables.Should().HaveSameCount(design.PersistentVariables);
        }

        [Fact]
        public void CreatesSettingsIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.Settings.Should().HaveSameCount(design.Settings);
        }

        [Fact]
        public void CreatesSystemInputsIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.SystemInputs.Should().HaveSameCount(design.SystemInputs);
        }

        [Fact]
        public void CreatesSystemOutputsIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.SystemOutputs.Should().HaveSameCount(design.SystemOutputs);
        }

        [Fact]
        public void CreatesProcessDataInputsIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.ProcessDataInputs.Should().HaveSameCount(design.ProcessDataInputs);
        }

        [Fact]
        public void CreatesProcessDataOutputsIfBlockIsCreated()
        {
            var design = CreateDesign();

            var editor = new FunctionBlockEditor(design);

            editor.FunctionBlock.ProcessDataOutputs.Should().HaveSameCount(design.ProcessDataOutputs);
        }

        [Fact]
        public void StoresSuppliedBlock()
        {
            var block = new FunctionBlock();

            var editor = new FunctionBlockEditor(block);

            editor.FunctionBlock.Should().BeSameAs(block);
        }
    }

    public class ResetToDefaults
    {
        [Fact]
        public void ResetsCycleFrequencyToDefault()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            editor.SetCycleFrequency(editor.FunctionBlock.CycleFrequency + 1);

            editor.ResetToDefaults();

            editor.FunctionBlock.CycleFrequency.Should().Be(design.DefaultCycleFrequency);
        }

        [Fact]
        public void ResetsRunModeToDefault()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            editor.SetRunMode(RunMode.Change);

            editor.ResetToDefaults();

            editor.FunctionBlock.RunMode.Should().Be(design.DefaultRunMode);
        }
    }

    public class SetCycleFrequency
    {
        [Fact]
        public void SetsCycleFrequency()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            uint cycleFrequency = 666;

            editor.SetCycleFrequency(cycleFrequency);

            editor.FunctionBlock.CycleFrequency.Should().Be(cycleFrequency);
        }
    }

    public class SetName
    {
        [Fact]
        public void SetsName()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            var nameToSet = "FortyTwo";

            editor.SetName(nameToSet);

            editor.FunctionBlock.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void TrimsName()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            var nameToSet = "   FortyTwo      ";
            var expectedName = "FortyTwo";

            editor.SetName(nameToSet);

            editor.FunctionBlock.Name.Should().Be(expectedName);
        }

        [Fact]
        public void CallsNameNotExistingValidationMethod()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design)
            {
                ContainerEditor = new MockContainerEditor()
            };
            var nameToSet = "FortyTwo";

            Action action = () => editor.SetName(nameToSet);

            action.Should().Throw<Exception>()
                .WithMessage(nameToSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNameIsEmptyOrWhiteSpace(string name)
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);

            Action action = () => editor.SetName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            var nameBefore = "FortyTwo";
            editor.SetName(nameBefore);

            try { editor.SetName("   "); }
            catch { }

            editor.FunctionBlock.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfNotExistingValidationFails()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            var nameBefore = "FortyTwo";
            editor.SetName(nameBefore);
            editor.ContainerEditor = new MockContainerEditor();

            try { editor.SetName("SixHundredSixtySix"); }
            catch { }

            editor.FunctionBlock.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotCallNameNotExistingValidationMethodIfSettingSameName()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            var nameBefore = "FortyTwo";
            editor.SetName(nameBefore);
            editor.ContainerEditor = new MockContainerEditor();

            Action action = () => editor.SetName(nameBefore);

            action.Should().NotThrow();
        }

        private sealed class MockContainerEditor : ContainerEditor
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Sufficient for the functionality to be tested here")]
            protected internal override void ValidateFunctionBlockOrContainerNameNotExisting(string name)
                => throw new Exception(name);
        }
    }

    public class SetRunMode
    {
        [Fact]
        public void SetsRunMode()
        {
            var design = CreateDesign();
            design.ValidRunModes = [RunMode.Cyclic, RunMode.Change];
            design.DefaultRunMode = RunMode.Cyclic;
            var editor = new FunctionBlockEditor(design);
            var runModeToSet = RunMode.Change;

            editor.SetRunMode(runModeToSet);

            editor.FunctionBlock.RunMode.Should().Be(runModeToSet);
        }

        [Fact]
        public void ThrowsIfRunModeIsNotValid()
        {
            var design = CreateDesign();
            var editor = new FunctionBlockEditor(design);
            var runModeToSet = RunMode.Cyclic;

            Action action = () => editor.SetRunMode(runModeToSet);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*not*valid*");
        }
    }

    private static FunctionBlockDesign CreateDesign()
    {
        var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);

        designEditor.SetName("design1");
        designEditor.SetDefaultCycleFrequency(1);

        var persistentVariableEditor = new PersistentVariableDesignEditor<int>("persistentVariable1");
        var settingEditor = new SettingDesignEditor<double>("settingDesign1", poolingFactory);
        var processDataInputEditor = new ConnectorDesignInputEditor<long>("input1", poolingFactory);
        var processDataOutputEditor = new ConnectorDesignOutputEditor<bool>("output1");

        designEditor.AddPersistentVariable(persistentVariableEditor);
        designEditor.AddSetting(settingEditor);
        designEditor.AddProcessDataInput(processDataInputEditor);
        designEditor.AddProcessDataOutput(processDataOutputEditor);

        return designEditor.Design;
    }
}
