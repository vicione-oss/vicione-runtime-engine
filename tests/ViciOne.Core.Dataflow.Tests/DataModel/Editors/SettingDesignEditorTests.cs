using AwesomeAssertions;
using System;
using System.Reflection;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class SettingDesignEditorTests
{
    protected PoolingFactory PoolingFactory { get; } = new PoolingFactory([Assembly.GetExecutingAssembly()]);

    public class AddValidator : SettingDesignEditorTests
    {
        [Fact]
        public void AddsValidator()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);
            var validatorToAdd = new MockValidator<object>();

            editor.AddValidator(validatorToAdd);

            editor.Setting.Validators.Should()
                .HaveCount(1).And
                .Contain(validatorToAdd);
        }

        [Fact]
        public void ThrowsIfValidatorWasAlreadyAdded()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);
            var validatorToAdd = new MockValidator<object>();
            editor.AddValidator(validatorToAdd);

            Action action = () => editor.AddValidator(validatorToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*validator*already*added*");
        }
    }

    public class Constructor : SettingDesignEditorTests
    {
        [Fact]
        public void CreatesSettingIfNotSupplied()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);

            editor.Setting.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfSettingIsCreated()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);

            editor.Setting.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void CreatesInputConnectorDesignIfSettingIsCreated()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);

            editor.Setting.Input.Should().NotBeNull();
        }

        [Fact]
        public void CreatesOutputConnectorDesignIfSettingIsCreated()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);

            editor.Setting.Output.Should().NotBeNull();
        }

        [Fact]
        public void SetsNameIfSettingIsCreated()
        {
            var nameToSet = "zweiundvierzig";

            var editor = new SettingDesignEditor<object>(nameToSet, PoolingFactory);

            editor.Setting.Name.Should().Be(nameToSet);
            editor.Setting.Input.Name.Should().Be(nameToSet);
            editor.Setting.Output.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void StoresSuppliedSetting()
        {
            var setting = new SettingDesign<object>
            {
                Input = new ConnectorDesignInput<object>(),
                Output = new ConnectorDesignOutput<object>()
            };

            var editor = new SettingDesignEditor<object>(setting);

            editor.Setting.Should().BeSameAs(setting);
        }
    }

    public class RemoveValidator : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void RemovessValidator()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            editor.RemoveValidator(validatorToRemove);

            editor.Setting.Validators.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsTrueIfValidatorWasRemoved()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfValidatorWasNotRemoved()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);
            var validatorToRemove = new MockValidator<object>();

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeFalse();
        }
    }

    public class SetDefaultValue : SettingDesignEditorTests
    {
        [Fact]
        public void SetsDefaultValue()
        {
            var valueToSet = 42;
            var editor = new SettingDesignEditor<int>("setting", PoolingFactory);

            editor.SetDefaultValue(valueToSet);

            editor.Setting.DefaultValue.Should().Be(valueToSet);
            editor.Setting.Input.DefaultValue.Should().Be(valueToSet);
            editor.Setting.Output.DefaultValue.Should().Be(valueToSet);
        }

        [Fact]
        public void UsesReturnValueOfValidator()
        {
            var editor = new SettingDesignEditor<int>("setting", PoolingFactory);
            var valueFromValidator = 666;
            var validator = new MockValidator<int>
            {
                UseSubstituteValue = true,
                SubstituteValue = valueFromValidator
            };
            editor.AddValidator(validator);

            editor.SetDefaultValue(42);

            editor.Setting.DefaultValue.Should().Be(valueFromValidator);
        }

        [Fact]
        public void DoesNotSetDefaultValueIfValidationFails()
        {
            var editor = new SettingDesignEditor<int>("setting", PoolingFactory);
            var valueBefore = 42;
            editor.SetDefaultValue(valueBefore);
            var validator = new MockValidator<int>
            {
                ThrowOnThrowIfInvalid = true
            };
            editor.AddValidator(validator);

            try { editor.SetDefaultValue(666); }
            catch { }

            editor.Setting.DefaultValue.Should().Be(valueBefore);
        }
    }

    public class SetName : SettingDesignEditorTests
    {
        [Fact]
        public void SetsName()
        {
            var nameToSet = "zweiundvierzig";
            var editor = new SettingDesignEditor<int>("setting", PoolingFactory);

            editor.SetName(nameToSet);

            editor.Setting.Name.Should().Be(nameToSet);
            editor.Setting.Input.Name.Should().Be(nameToSet);
            editor.Setting.Output.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void TrimsName()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);
            var nameToSet = "   Zweiundvierzig      ";
            var expectedName = "Zweiundvierzig";

            editor.SetName(nameToSet);

            editor.Setting.Name.Should().Be(expectedName);
            editor.Setting.Input.Name.Should().Be(expectedName);
            editor.Setting.Output.Name.Should().Be(expectedName);
        }

        [Fact]
        public void CallsNameNotExistingValidationMethod()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);
            ((ISettingDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(PoolingFactory);
            var nameToSet = "Zweiundvierzig";

            Action action = () => editor.SetName(nameToSet);

            action.Should().Throw<Exception>()
                .WithMessage(nameToSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNameIsEmptyOrWhiteSpace(string name)
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);

            Action action = () => editor.SetName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNameIsNotValidNamespaceOrClassName()
        {
            var editor = new SettingDesignEditor<object>("setting", PoolingFactory);

            Action action = () => editor.SetName("Zwei und vierzig");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new SettingDesignEditor<object>(nameBefore, PoolingFactory);

            try { editor.SetName("   "); }
            catch { }

            editor.Setting.Name.Should().Be(nameBefore);
            editor.Setting.Input.Name.Should().Be(nameBefore);
            editor.Setting.Output.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfNotExistingValidationFails()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new SettingDesignEditor<object>(nameBefore, PoolingFactory);
            ((ISettingDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(PoolingFactory);

            try { editor.SetName("Sechshundertsechsundsechzig"); }
            catch { }

            editor.Setting.Name.Should().Be(nameBefore);
            editor.Setting.Input.Name.Should().Be(nameBefore);
            editor.Setting.Output.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsValidNamespaceOrClassNameValidationFails()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new SettingDesignEditor<object>(nameBefore, PoolingFactory);

            try { editor.SetName("Zwei und vierzig"); }
            catch { }

            editor.Setting.Name.Should().Be(nameBefore);
            editor.Setting.Input.Name.Should().Be(nameBefore);
            editor.Setting.Output.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotCallNameNotExistingValidationMethodIfSettingSameName()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new SettingDesignEditor<object>(nameBefore, PoolingFactory);
            ((ISettingDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(PoolingFactory);

            Action action = () => editor.SetName(nameBefore);

            action.Should().NotThrow();
        }

        private sealed class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Sufficient for the functionality to be tested here")]
            internal override void ValidateSettingDesignNameNotExisting(string name)
                => throw new Exception(name);
        }
    }
}
