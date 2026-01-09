using AwesomeAssertions;
using System;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class PersistentVariableEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesVariableIfNotSupplied()
        {
            var design = new PersistentVariableDesign<object>();

            var editor = new PersistentVariableEditor<object>(design);

            editor.PersistentVariable.Should().NotBeNull();
            editor.PersistentVariable.Design.Should().Be(design);
        }

        [Fact]
        public void SetsNewIdIfVariableIsCreated()
        {
            var design = new PersistentVariableDesign<object>();

            var editor = new PersistentVariableEditor<object>(design);

            editor.PersistentVariable.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsDefaultValueIfVariableIsCreated()
        {
            var design = new PersistentVariableDesign<int>
            {
                DefaultValue = 42
            };

            var editor = new PersistentVariableEditor<int>(design);

            editor.PersistentVariable.Value.Should().Be(design.DefaultValue);
        }

        [Fact]
        public void StoresSuppliedVariable()
        {
            var variable = new PersistentVariable<object>();

            var editor = new PersistentVariableEditor<object>(variable);

            editor.PersistentVariable.Should().BeSameAs(variable);
        }
    }

    public class ResetToDefaults
    {
        [Fact]
        public void ResetsValueToDefault()
        {
            var design = new PersistentVariableDesign<int>
            {
                DefaultValue = 42
            };
            var editor = new PersistentVariableEditor<int>(design);
            editor.SetValue(666);

            editor.ResetToDefaults();

            editor.PersistentVariable.Value.Should().Be(design.DefaultValue);
        }
    }

    public class SetValue
    {
        [Fact]
        public void SetsValue()
        {
            var design = new PersistentVariableDesign<int>();
            var editor = new PersistentVariableEditor<int>(design);
            var valueToSet = 42;

            editor.SetValue(valueToSet);

            editor.PersistentVariable.Value.Should().Be(valueToSet);
        }

        [Fact]
        public void SetsChangedTimeIfValueIsDifferent()
        {
            var design = new PersistentVariableDesign<int>();
            var editor = new PersistentVariableEditor<int>(design);
            var valueToSet = 42;

            editor.SetValue(valueToSet);

            editor.PersistentVariable.ChangedTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void DoesNotSetChangedTimeIfValueIsSame()
        {
            var design = new PersistentVariableDesign<int>();
            var editor = new PersistentVariableEditor<int>(design);
            var valueToSet = 42;
            var changedTimeBefore = DateTime.MinValue;
            editor.PersistentVariable.Value = valueToSet;
            editor.PersistentVariable.ChangedTime = changedTimeBefore;

            editor.SetValue(valueToSet);

            editor.PersistentVariable.ChangedTime.Should().Be(changedTimeBefore);
        }
    }
}
