using AwesomeAssertions;
using System;
using System.Reflection;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class SettingEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesSettingIfNotSupplied()
        {
            var design = CreateDesign<object>();

            var editor = new SettingEditor<object>(design);

            editor.Setting.Should().NotBeNull();
            editor.Setting.Design.Should().Be(design);
        }

        [Fact]
        public void SetsNewIdIfSettingIsCreated()
        {
            var design = CreateDesign<object>();

            var editor = new SettingEditor<object>(design);

            editor.Setting.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void CreatesInputConnector()
        {
            var design = CreateDesign<object>();

            var editor = new SettingEditor<object>(design);

            editor.Setting.Input.Should().NotBeNull();
        }

        [Fact]
        public void CreatesOutputConnector()
        {
            var design = CreateDesign<object>();

            var editor = new SettingEditor<object>(design);

            editor.Setting.Output.Should().NotBeNull();
        }

        [Fact]
        public void SetsDefaultValue()
        {
            var design = CreateDesign<int>();
            design.DefaultValue = 42;

            var editor = new SettingEditor<int>(design);

            editor.Setting.Value.Should().Be(design.DefaultValue);
        }

        [Fact]
        public void DoesNotSetChangedTime()
        {
            var design = CreateDesign<int>();
            design.DefaultValue = 42;

            var editor = new SettingEditor<int>(design);

            editor.Setting.ChangedTime.Should().Be(default);
        }

        [Fact]
        public void StoresSuppliedSetting()
        {
            var setting = new Setting<object>
            {
                Input = new ConnectorInput<object>(),
                Output = new ConnectorOutput<object>()
            };

            var editor = new SettingEditor<object>(setting);

            editor.Setting.Should().BeSameAs(setting);
        }
    }

    public class ResetToDefaults
    {
        [Fact]
        public void ResetsValueToDefault()
        {
            var design = CreateDesign<int>();
            design.DefaultValue = 42;
            var editor = new SettingEditor<int>(design);
            editor.SetValue(666);

            editor.ResetToDefaults();

            editor.Setting.Value.Should().Be(design.DefaultValue);
        }
    }

    public class SetValue
    {
        [Fact]
        public void SetsValue()
        {
            var design = CreateDesign<int>();
            var editor = new SettingEditor<int>(design);
            var valueToSet = 42;

            editor.SetValue(valueToSet);

            editor.Setting.Value.Should().Be(valueToSet);
            editor.Setting.Input.Value.Should().Be(valueToSet);
            editor.Setting.Output.Value.Should().Be(valueToSet);
        }

        [Fact]
        public void UsesReturnValueOfValidator()
        {
            var design = CreateDesign<int>();
            var valueFromValidator = 666;
            var validator = new MockValidator<int>
            {
                UseSubstituteValue = true,
                SubstituteValue = valueFromValidator
            };
            design.Validators.Add(validator);
            var editor = new SettingEditor<int>(design);

            editor.SetValue(42);

            editor.Setting.Value.Should().Be(valueFromValidator);
        }

        [Fact]
        public void DoesNotSetValueIfValidationFails()
        {
            var design = CreateDesign<int>();
            var validator = new MockValidator<int>
            {
                ThrowOnThrowIfInvalid = true
            };
            var editor = new SettingEditor<int>(design);
            var valueBefore = 42;
            editor.SetValue(valueBefore);
            design.Validators.Add(validator);


            try { editor.SetValue(666); }
            catch { }

            editor.Setting.Value.Should().Be(valueBefore);
        }

        [Fact]
        public void SetsChangedTimeIfValueIsDifferent()
        {
            var design = CreateDesign<int>();
            var editor = new SettingEditor<int>(design);
            var valueToSet = 42;

            editor.SetValue(valueToSet);

            editor.Setting.ChangedTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void DoesNotSetChangedTimeIfValueIsSame()
        {
            var design = CreateDesign<int>();
            var editor = new SettingEditor<int>(design);
            var valueToSet = 42;
            var changedTimeBefore = DateTime.MinValue;
            editor.Setting.Value = valueToSet;
            editor.Setting.ChangedTime = changedTimeBefore;

            editor.SetValue(valueToSet);

            editor.Setting.ChangedTime.Should().Be(changedTimeBefore);
        }
    }

    private static SettingDesign<TValue> CreateDesign<TValue>()
    {
        var name = "setting";
        var design = new SettingDesign<TValue>
        {
            Name = name
        };
        var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
        design.Input = new ConnectorDesignInputEditor<TValue>(name, poolingFactory).Design;
        design.Output = new ConnectorDesignOutputEditor<TValue>(name).Design;
        return design;
    }
}
