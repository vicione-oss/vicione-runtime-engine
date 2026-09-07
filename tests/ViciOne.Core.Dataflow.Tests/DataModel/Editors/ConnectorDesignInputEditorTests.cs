using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class ConnectorDesignInputEditorTests
{
    protected PoolingFactory PoolingFactory { get; } = new PoolingFactory([Assembly.GetExecutingAssembly()]);

    public class AddValidator : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void AddsValidator()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var validatorToAdd = new MockValidator<object>();

            editor.AddValidator(validatorToAdd);

            editor.Design.Validators.Should()
                .HaveCount(1).And
                .Contain(validatorToAdd);
        }

        [Fact]
        public void ThrowsIfValidatorWasAlreadyAdded()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var validatorToAdd = new MockValidator<object>();
            editor.AddValidator(validatorToAdd);

            Action action = () => editor.AddValidator(validatorToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*validator*already*added*");
        }
    }

    public class Constructor : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void CreatesDesignIfNotSupplied()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            editor.Design.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfDesignIsCreated()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            editor.Design.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsNameIfDesignIsCreated()
        {
            var nameToSet = "fortytwo";

            var editor = new ConnectorDesignInputEditor<object>(nameToSet, PoolingFactory);

            editor.Design.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void SetsDefaultPoolingStrategiesToQueueIfDesignIsCreatedAndNoDefaultPoolingAvailable()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            editor.Design.DefaultCrossSourcePoolingStrategy.Should().Be(PoolingStrategy.Queue);
            editor.Design.DefaultPerSourcePoolingStrategy.Should().Be(PoolingStrategy.Queue);
        }

        [Fact]
        public void SetsDefaultPoolingStrategiesToAggregateIfDesignIsCreatedAndDefaultPoolingAvailable()
        {
            var editor = new ConnectorDesignInputEditor<Constructor>("design", PoolingFactory);

            editor.Design.DefaultCrossSourcePoolingStrategy.Should().Be(PoolingStrategy.Aggregate);
            editor.Design.DefaultPerSourcePoolingStrategy.Should().Be(PoolingStrategy.Aggregate);
        }

        [Fact]
        public void SetsNewDefaultAggregatingPoolingsIfDesignIsCreatedAndDefaultPoolingAvailable()
        {
            var editor = new ConnectorDesignInputEditor<Constructor>("design", PoolingFactory);

            editor.Design.DefaultCrossSourceAggregatingPooling.Should().BeOfType<MockPooling>();
            editor.Design.DefaultPerSourceAggregatingPooling.Should().BeOfType<MockPooling>();
        }

        [Fact]
        public void CreatesDifferentObjectInstancesForDefaultAggregatingPoolings()
        {
            var editor = new ConnectorDesignInputEditor<Constructor>("design", PoolingFactory);

            editor.Design.DefaultCrossSourceAggregatingPooling.Should().NotBeSameAs(editor.Design.DefaultPerSourceAggregatingPooling);
        }

        [Fact]
        public void StoresSuppliedDesign()
        {
            var design = new ConnectorDesignInput<object>();

            var editor = new ConnectorDesignInputEditor<object>(design);

            editor.Design.Should().BeSameAs(design);
        }

        [DefaultPoolingForType<Constructor>]
        public class MockPooling : IAggregatingPooling<Constructor>
        {
            public string Description => string.Empty;
            public string Name => string.Empty;

            public Constructor? Aggregate(IEnumerable<Constructor?> values) => throw new NotImplementedException();
        }
    }

    public class RemoveValidator : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void RemovessValidator()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            editor.RemoveValidator(validatorToRemove);

            editor.Design.Validators.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsTrueIfValidatorWasRemoved()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfValidatorWasNotRemoved()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var validatorToRemove = new MockValidator<object>();

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeFalse();
        }
    }

    public class SetDefaultCrossSourceAggregatingPooling : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void SetsDefaultCrossSourceAggregatingPooling()
        {
            var pooling = Substitute.For<IAggregatingPooling<object>>();
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            editor.SetDefaultCrossSourceAggregatingPooling(pooling);

            editor.Design.DefaultCrossSourceAggregatingPooling.Should().BeSameAs(pooling);
        }

        [Fact]
        public void ThrowsIfSettingNullAndStrategyIsAggregate()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            editor.Design.DefaultCrossSourcePoolingStrategy = PoolingStrategy.Aggregate;

            Action action = () => editor.SetDefaultCrossSourceAggregatingPooling(null);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*pooling*not*null*");
        }

        [Fact]
        public void DoesNotThrowIfSettingNullAndStrategyIsNotAggregate()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            editor.Design.DefaultCrossSourcePoolingStrategy = PoolingStrategy.Queue;

            Action action = () => editor.SetDefaultCrossSourceAggregatingPooling(null);

            action.Should().NotThrow();
        }
    }

    public class SetDefaultPerSourceAggregatingPooling : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void SetsDefaultPerSourceAggregatingPooling()
        {
            var pooling = Substitute.For<IAggregatingPooling<object>>();
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            editor.SetDefaultPerSourceAggregatingPooling(pooling);

            editor.Design.DefaultPerSourceAggregatingPooling.Should().BeSameAs(pooling);
        }

        [Fact]
        public void ThrowsIfSettingNullAndStrategyIsAggregate()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            editor.Design.DefaultPerSourcePoolingStrategy = PoolingStrategy.Aggregate;

            Action action = () => editor.SetDefaultPerSourceAggregatingPooling(null);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*pooling*not*null*");
        }

        [Fact]
        public void DoesNotThrowIfSettingNullAndStrategyIsNotAggregate()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            editor.Design.DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue;

            Action action = () => editor.SetDefaultPerSourceAggregatingPooling(null);

            action.Should().NotThrow();
        }
    }

    public class SetDefaultCrossSourcePoolingStrategy : ConnectorDesignInputEditorTests
    {
        [Theory]
        [InlineData(PoolingStrategy.Aggregate)]
        [InlineData(PoolingStrategy.Concat)]
        [InlineData(PoolingStrategy.Queue)]
        public void SetsDefaultCrossSourcePoolingStrategy(PoolingStrategy strategy)
        {
            var editor = new ConnectorDesignInputEditor<object[]>("design", PoolingFactory);

            editor.SetDefaultCrossSourcePoolingStrategy(strategy, strategy == PoolingStrategy.Aggregate ? Substitute.For<IAggregatingPooling<object[]>>() : null);

            editor.Design.DefaultCrossSourcePoolingStrategy.Should().Be(strategy);
        }

        [Fact]
        public void SetsDefaultCrossSourceAggregatingPooling()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var aggregatingPoolingToSet = Substitute.For<IAggregatingPooling<object>>();

            editor.SetDefaultCrossSourcePoolingStrategy(PoolingStrategy.Aggregate, aggregatingPoolingToSet);

            editor.Design.DefaultCrossSourceAggregatingPooling.Should().BeSameAs(aggregatingPoolingToSet);
        }

        [Fact]
        public void ThrowsIfSettingConcatStrategyButTypeIsNoArray()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            Action action = () => editor.SetDefaultCrossSourcePoolingStrategy(PoolingStrategy.Concat);

            action.Should().Throw<ArgumentException>()
                .WithMessage($"*{PoolingStrategy.Concat}*must*array*");
        }
    }

    public class SetDefaultPerSourcePoolingStrategy : ConnectorDesignInputEditorTests
    {
        [Theory]
        [InlineData(PoolingStrategy.Aggregate)]
        [InlineData(PoolingStrategy.Concat)]
        [InlineData(PoolingStrategy.Queue)]
        public void SetsDefaultPerSourcePoolingStrategy(PoolingStrategy strategy)
        {
            var editor = new ConnectorDesignInputEditor<object[]>("design", PoolingFactory);

            editor.SetDefaultPerSourcePoolingStrategy(strategy, strategy == PoolingStrategy.Aggregate ? Substitute.For<IAggregatingPooling<object[]>>() : null);

            editor.Design.DefaultPerSourcePoolingStrategy.Should().Be(strategy);
        }

        [Fact]
        public void SetsDefaultPerSourceAggregatingPooling()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var aggregatingPoolingToSet = Substitute.For<IAggregatingPooling<object>>();

            editor.SetDefaultPerSourcePoolingStrategy(PoolingStrategy.Aggregate, aggregatingPoolingToSet);

            editor.Design.DefaultPerSourceAggregatingPooling.Should().BeSameAs(aggregatingPoolingToSet);
        }

        [Fact]
        public void ThrowsIfSettingConcatStrategyButTypeIsNoArray()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            Action action = () => editor.SetDefaultPerSourcePoolingStrategy(PoolingStrategy.Concat);

            action.Should().Throw<ArgumentException>()
                .WithMessage($"*{PoolingStrategy.Concat}*must*array*");
        }
    }

    public class SetDefaultValue : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void SetsDefaultValue()
        {
            var editor = new ConnectorDesignInputEditor<int>("design", PoolingFactory);
            var valueToSet = 42;

            editor.SetDefaultValue(valueToSet);

            editor.Design.DefaultValue.Should().Be(valueToSet);
        }

        [Fact]
        public void UsesReturnValueOfValidator()
        {
            var editor = new ConnectorDesignInputEditor<int>("design", PoolingFactory);
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
            var editor = new ConnectorDesignInputEditor<int>("design", PoolingFactory);
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

    public class SetName : ConnectorDesignInputEditorTests
    {
        [Fact]
        public void SetsName()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var nameToSet = "FortyTwo";

            editor.SetName(nameToSet);

            editor.Design.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void TrimsName()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            var nameToSet = "   FortyTwo      ";
            var expectedName = "FortyTwo";

            editor.SetName(nameToSet);

            editor.Design.Name.Should().Be(expectedName);
        }

        [Fact]
        public void CallsNameNotExistingValidationMethod()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);
            ((IConnectorDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(PoolingFactory);
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
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            Action action = () => editor.SetName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNameIsNotValidNamespaceOrClassName()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            Action action = () => editor.SetName("Forty two");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void ThrowsIfNameIsSystemReserved()
        {
            var editor = new ConnectorDesignInputEditor<object>("design", PoolingFactory);

            Action action = () => editor.SetName(SystemConnectorNames.FunctionBlockEnabledName);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*reserved*system*name*");
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignInputEditor<object>(nameBefore, PoolingFactory);

            try { editor.SetName("   "); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfNotExistingValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignInputEditor<object>(nameBefore, PoolingFactory);
            ((IConnectorDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(PoolingFactory);

            try { editor.SetName("SixHundredSixtySix"); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsValidNamespaceOrClassNameValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignInputEditor<object>(nameBefore, PoolingFactory);

            try { editor.SetName("Forty two"); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsNotSystemReservedValidationFails()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignInputEditor<object>(nameBefore, PoolingFactory);

            try { editor.SetName(SystemConnectorNames.FunctionBlockEnabledName); }
            catch { }

            editor.Design.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotCallNameNotExistingValidationMethodIfSettingSameName()
        {
            var nameBefore = "FortyTwo";
            var editor = new ConnectorDesignInputEditor<object>(nameBefore, PoolingFactory);
            ((IConnectorDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(PoolingFactory);

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
