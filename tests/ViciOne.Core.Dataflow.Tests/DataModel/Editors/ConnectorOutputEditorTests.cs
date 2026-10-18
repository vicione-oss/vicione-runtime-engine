using AwesomeAssertions;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class ConnectorOutputEditorTests
{
    public class AddValidator
    {
        [Fact]
        public void AddsValidator()
        {
            var design = new ConnectorDesignOutput<object>();
            var editor = new ConnectorOutputEditor<object>(design);
            var validatorToAdd = new MockValidator<object>();

            editor.AddValidator(validatorToAdd);

            editor.Connector.Validators.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeSameAs(validatorToAdd);
        }

        [Fact]
        public void ThrowsIfValidatorWasAlreadyAdded()
        {
            var design = new ConnectorDesignOutput<object>();
            var editor = new ConnectorOutputEditor<object>(design);
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
        public void CreatesConnectorIfNotSupplied()
        {
            var design = new ConnectorDesignOutput<object>();

            var editor = new ConnectorOutputEditor<object>(design);

            editor.Connector.Should().NotBeNull();
            editor.Connector.Design.Should().Be(design);
        }

        [Fact]
        public void SetsNewIdIfConnectorIsCreated()
        {
            var design = new ConnectorDesignOutput<object>();

            var editor = new ConnectorOutputEditor<object>(design);

            editor.Connector.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsMarkAsChangedOnlyIfNotEqualToDefaultIfConnectorIsCreated()
        {
            var design = new ConnectorDesignOutput<object>();

            var editor = new ConnectorOutputEditor<object>(design);

            editor.Connector.MarkAsChangedOnlyIfNotEqual.Should().Be(ConnectorDefaults.MarkAsChangedOnlyIfNotEqual);
        }

        [Fact]
        public void SetsDefaultValueIfConnectorIsCreated()
        {
            var design = new ConnectorDesignOutput<object>();

            var editor = new ConnectorOutputEditor<object>(design);

            editor.Connector.Value.Should().BeSameAs(design.DefaultValue);
        }

        [Fact]
        public void StoresSuppliedConnector()
        {
            var connector = new ConnectorOutput<object>();

            var editor = new ConnectorOutputEditor<object>(connector);

            editor.Connector.Should().BeSameAs(connector);
        }
    }

    public class RemoveValidator
    {
        [Fact]
        public void RemovessValidator()
        {
            var design = new ConnectorDesignOutput<object>();
            var editor = new ConnectorOutputEditor<object>(design);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            editor.RemoveValidator(validatorToRemove);

            editor.Connector.Validators.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsTrueIfValidatorWasRemoved()
        {
            var design = new ConnectorDesignOutput<object>();
            var editor = new ConnectorOutputEditor<object>(design);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfValidatorWasNotRemoved()
        {
            var design = new ConnectorDesignOutput<object>();
            var editor = new ConnectorOutputEditor<object>(design);
            var validatorToRemove = new MockValidator<object>();

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeFalse();
        }
    }

    public class ResetToDefaults
    {
        [Fact]
        public void SetsDefaultValue()
        {
            var design = new ConnectorDesignOutput<object>();
            var editor = new ConnectorOutputEditor<object>(design);
            editor.Connector.Value = new object();

            editor.ResetToDefaults();

            editor.Connector.Value.Should().BeSameAs(design.DefaultValue);
        }
    }

    public class SetMarkAsChangedOnlyIfNotEqual
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetsMarkAsChangedOnlyIfNotEqual(bool expectedValue)
        {
            var design = new ConnectorDesignOutput<object>();
            var editor = new ConnectorOutputEditor<object>(design);
            editor.Connector.MarkAsChangedOnlyIfNotEqual = expectedValue == false;

            editor.SetMarkAsChangedOnlyIfNotEqual(expectedValue);

            editor.Connector.MarkAsChangedOnlyIfNotEqual.Should().Be(expectedValue);
        }
    }

    public class SetValue
    {
        [Fact]
        public void SetsValue()
        {
            var design = new ConnectorDesignOutput<int>();
            var editor = new ConnectorOutputEditor<int>(design);
            var valueToSet = 42;

            editor.SetValue(valueToSet);

            editor.Connector.Value.Should().Be(valueToSet);
        }

        [Fact]
        public void UsesReturnValueOfDesignValidator()
        {
            var design = new ConnectorDesignOutput<int>();
            var editor = new ConnectorOutputEditor<int>(design);
            var validator = new MockValidator<int>
            {
                SubstituteValue = 42,
                UseSubstituteValue = true
            };
            design.Validators.Add(validator);

            editor.SetValue(666);

            editor.Connector.Value.Should().Be(validator.SubstituteValue);
        }

        [Fact]
        public void UsesReturnValueOfConnectorValidator()
        {
            var design = new ConnectorDesignOutput<int>();
            var editor = new ConnectorOutputEditor<int>(design);
            var validator = new MockValidator<int>
            {
                SubstituteValue = 42,
                UseSubstituteValue = true
            };
            editor.Connector.Validators.Add(validator);

            editor.SetValue(666);

            editor.Connector.Value.Should().Be(validator.SubstituteValue);
        }

        public static TheoryData<int, List<IValidator<int>>, List<IValidator<int>>, int> ArgumentsFor_PassesValueToValidators => new()
        {
            { 1, [new AddOneValidator()], [], 2 },
            { 1, [], [new AddOneValidator()], 2 },
            { 1, [new AddOneValidator()], [new AddOneValidator()], 3 },
            { 1, [new AddOneValidator()], [new AddOneValidator(), new AddOneValidator()], 4 },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_PassesValueToValidators))]
        public void PassesValueToValidators(int valueToSet, List<IValidator<int>> designValidators, List<IValidator<int>> connectorValidators, int expectedValue)
        {
            var design = new ConnectorDesignOutput<int>();
            var editor = new ConnectorOutputEditor<int>(design);
            design.Validators = designValidators;
            editor.Connector.Validators = connectorValidators;

            editor.SetValue(valueToSet);

            editor.Connector.Value.Should().Be(expectedValue);
        }

        private sealed class AddOneValidator : IValidator<int>
        {
            public int Validate(int value, bool throwIfInvalid) => value + 1;
            object? IValidator.Validate(object? value, bool throwIfInvalid)
                => Validate((int)value!, throwIfInvalid);
        }
    }
}
