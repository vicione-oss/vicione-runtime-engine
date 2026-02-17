using AwesomeAssertions;
using System;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class ConnectorDesignOutputEditorTests
{
    public class AddValidator
    {
        [Fact]
        public void AddsValidator()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var validatorToAdd = new MockValidator<object>();

            editor.AddValidator(validatorToAdd);

            editor.Design.Validators.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeSameAs(validatorToAdd);
        }

        [Fact]
        public void ThrowsIfValidatorWasAlreadyAdded()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var validatorToAdd = new MockValidator<object>();
            editor.AddValidator(validatorToAdd);

            Action action = () => editor.AddValidator(validatorToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*validator*already*added*");
        }
    }

    public class Constructor
    {
        [Fact]
        public void CreatesDesignIfNotSupplied()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");

            editor.Design.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfDesignIsCreated()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");

            editor.Design.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsNameIfDesignIsCreated()
        {
            var nameToSet = "fortytwo";

            var editor = new ConnectorDesignOutputEditor<object>(nameToSet);

            editor.Design.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void StoresSuppliedDesign()
        {
            var design = new ConnectorDesignOutput<object>();

            var editor = new ConnectorDesignOutputEditor<object>(design);

            editor.Design.Should().BeSameAs(design);
        }
    }

    public class RemoveValidator
    {
        [Fact]
        public void RemovessValidator()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            editor.RemoveValidator(validatorToRemove);

            editor.Design.Validators.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsTrueIfValidatorWasRemoved()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfValidatorWasNotRemoved()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var validatorToRemove = new MockValidator<object>();

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeFalse();
        }
    }

    public class SetDefaultValue
    {
        [Fact]
        public void SetsDefaultValue()
        {
            var editor = new ConnectorDesignOutputEditor<int>("design");
            var valueToSet = 42;

            editor.SetDefaultValue(valueToSet);

            editor.Design.DefaultValue.Should().Be(valueToSet);
        }

        [Fact]
        public void UsesReturnValueOfValidator()
        {
            var editor = new ConnectorDesignOutputEditor<int>("design");
            var valueFromValidator = 666;
            var validator = new MockValidator<int>
            {
                UseSubstituteValue = true,
                SubstituteValue = valueFromValidator
            };
            editor.AddValidator(validator);

            editor.SetDefaultValue(42);

            editor.Design.DefaultValue.Should().Be(valueFromValidator);
        }

        [Fact]
        public void DoesNotSetDefaultValueIfValidationFails()
        {
            var editor = new ConnectorDesignOutputEditor<int>("design");
            var valueBefore = 42;
            editor.SetDefaultValue(valueBefore);
            var validator = new MockValidator<int>
            {
                ThrowOnThrowIfInvalid = true
            };
            editor.AddValidator(validator);

            try { editor.SetDefaultValue(666); }
            catch { }

            editor.Design.DefaultValue.Should().Be(valueBefore);
        }
    }

    public class SetName
    {
        [Fact]
        public void SetsName()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var nameToSet = "FortyTwo";

            editor.SetName(nameToSet);

            editor.Design.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void TrimsName()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var nameToSet = "   FortyTwo      ";
            var expectedName = "FortyTwo";

            editor.SetName(nameToSet);

            editor.Design.Name.Should().Be(expectedName);
        }

        [Fact]
        public void CallsNameNotExistingValidationMethod()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            ((IConnectorDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(poolingFactory);
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
            var editor = new ConnectorDesignOutputEditor<object>("design");

            Action action = () => editor.SetName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNameIsNotValidNamespaceOrClassName()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");

            Action action = () => editor.SetName("Forty two");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void ThrowsIfNameIsSystemReserved()
        {
            var editor = new ConnectorDesignOutputEditor<object>("design");

            Action action = () => editor.SetName(SystemConnectorNames.FunctionBlockEnabledName);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*reserved*system*name*");
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignOutputEditor<object>(nameBefore);

            try { editor.SetName("   "); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfNotExistingValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignOutputEditor<object>(nameBefore);
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            ((IConnectorDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(poolingFactory);

            try { editor.SetName("SixHundredSixtySix"); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsValidNamespaceOrClassNameValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignOutputEditor<object>(nameBefore);

            try { editor.SetName("Forty two"); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsNotSystemReservedValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignOutputEditor<object>(nameBefore);

            try { editor.SetName(SystemConnectorNames.FunctionBlockEnabledName); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotCallNameNotExistingValidationMethodIfSettingSameName()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignOutputEditor<object>(nameBefore);
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            ((IConnectorDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(poolingFactory);

            Action action = () => editor.SetName(nameBefore);

            action.Should().NotThrow();
        }

        private sealed class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Sufficient for the functionality to be tested here")]
            internal override void ValidateConnectorDesignNameNotExisting(ConnectorDesign design, string name)
                => throw new Exception(name);
        }
    }
}
