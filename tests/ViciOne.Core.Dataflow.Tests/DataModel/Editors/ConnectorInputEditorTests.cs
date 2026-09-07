using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class ConnectorInputEditorTests
{
    public class AddValidator
    {
        [Fact]
        public void AddsValidator()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var validatorToAdd = new MockValidator<object>();

            editor.AddValidator(validatorToAdd);

            editor.Connector.Validators.Should()
                .HaveCount(1).And
                .Contain(validatorToAdd);
        }

        [Fact]
        public void ThrowsIfValidatorWasAlreadyAdded()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
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
            var design = CreateDesign<object>();

            var editor = new ConnectorInputEditor<object>(design);

            editor.Connector.Should().NotBeNull();
            editor.Connector.Design.Should().Be(design);
        }

        [Fact]
        public void SetsNewIdIfConnectorIsCreated()
        {
            var design = CreateDesign<object>();

            var editor = new ConnectorInputEditor<object>(design);

            editor.Connector.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsMarkAsChangedOnlyIfNotEqualToDefaultIfConnectorIsCreated()
        {
            var design = CreateDesign<object>();

            var editor = new ConnectorInputEditor<object>(design);

            editor.Connector.MarkAsChangedOnlyIfNotEqual.Should().Be(ConnectorDefaults.MarkAsChangedOnlyIfNotEqual);
        }

        [Fact]
        public void SetsCrossSourcePoolingStrategyIfConnectorIsCreated()
        {
            var design = CreateDesign<object[]>(PoolingStrategy.Concat);

            var editor = new ConnectorInputEditor<object[]>(design);

            editor.Connector.CrossSourcePoolingStrategy.Should().Be(design.DefaultCrossSourcePoolingStrategy);
        }

        [Fact]
        public void SetsPerSourcePoolingStrategyIfConnectorIsCreated()
        {
            var design = CreateDesign<object[]>(PoolingStrategy.Concat);

            var editor = new ConnectorInputEditor<object[]>(design);

            editor.Connector.PerSourcePoolingStrategy.Should().Be(design.DefaultPerSourcePoolingStrategy);
        }

        [Fact]
        public void ClonesCrossSourceAggregatingPoolingIfConnectorIsCreated()
        {
            var stringToClone = "fortytwo";
            var arrayToClone = new[] { "sixhundredsixtysix" };
            var child = new ChildPropertyType()
            {
                Id = Guid.NewGuid(),
                AnotherChild = new()
                {
                    Id = Guid.NewGuid(),
                },
            };
            var originalPooling = new PoolingToClone<object>()
            {
                StringPropertyToClone = stringToClone,
                ArrayPropertyToClone = arrayToClone,
                ClassProperty = child,
            };
            var design = CreateDesign(crossSourcePooling: originalPooling);

            var editor = new ConnectorInputEditor<object>(design);

            editor.Connector.CrossSourceAggregatingPooling.Should().BeOfType(design.DefaultCrossSourceAggregatingPooling!.GetType());
            editor.Connector.CrossSourceAggregatingPooling.Should().NotBeSameAs(design.DefaultCrossSourceAggregatingPooling);
            var clonedPooling = (PoolingToClone<object>)editor.Connector.CrossSourceAggregatingPooling!;
            clonedPooling.StringPropertyToClone.Should().Be(stringToClone);
            clonedPooling.ArrayPropertyToClone.Should().NotBeSameAs(arrayToClone);
            clonedPooling.ArrayPropertyToClone.Should().BeEquivalentTo(arrayToClone);
            clonedPooling.ClassProperty.Should().NotBeSameAs(child);
            clonedPooling.ClassProperty.Should().BeEquivalentTo(child);
        }

        [Fact]
        public void ClonesPerSourceAggregatingPoolingIfConnectorIsCreated()
        {
            var stringToClone = "sixhundredsixtysix";
            var arrayToClone = new[] { "fortytwo" };
            var child = new ChildPropertyType()
            {
                Id = Guid.NewGuid(),
                AnotherChild = new()
                {
                    Id = Guid.NewGuid(),
                },
            };
            var originalPooling = new PoolingToClone<object>()
            {
                StringPropertyToClone = stringToClone,
                ArrayPropertyToClone = arrayToClone,
                ClassProperty = child,
            };
            var design = CreateDesign(perSourcePooling: originalPooling);

            var editor = new ConnectorInputEditor<object>(design);

            editor.Connector.PerSourceAggregatingPooling.Should().BeOfType(design.DefaultPerSourceAggregatingPooling!.GetType());
            editor.Connector.PerSourceAggregatingPooling.Should().NotBeSameAs(design.DefaultPerSourceAggregatingPooling);
            var clonedPooling = (PoolingToClone<object>)editor.Connector.PerSourceAggregatingPooling!;
            clonedPooling.StringPropertyToClone.Should().Be(stringToClone);
            clonedPooling.ArrayPropertyToClone.Should().NotBeSameAs(arrayToClone);
            clonedPooling.ArrayPropertyToClone.Should().BeEquivalentTo(arrayToClone);
            clonedPooling.ClassProperty.Should().NotBeSameAs(child);
            clonedPooling.ClassProperty.Should().BeEquivalentTo(child);
        }

        [Fact]
        public void SetsDefaultValueIfConnectorIsCreated()
        {
            var design = CreateDesign<object>();

            var editor = new ConnectorInputEditor<object>(design);

            editor.Connector.Value.Should().BeSameAs(design.DefaultValue);
        }

        [Fact]
        public void StoresSuppliedConnector()
        {
            var connector = new ConnectorInput<object>();

            var editor = new ConnectorInputEditor<object>(connector);

            editor.Connector.Should().BeSameAs(connector);
        }

        private sealed class PoolingToClone<T> : IAggregatingPooling<T>
        {
            public string Description => string.Empty;
            public string Name => string.Empty;
            public string StringPropertyToClone { get; set; } = string.Empty;
            public string[] ArrayPropertyToClone { get; set; } = [];
            public ChildPropertyType ClassProperty { get; set; } = new();

            public T Aggregate(IEnumerable<T?> values) => throw new NotImplementedException();
        }

        private sealed class ChildPropertyType
        {
            public ChildPropertyType? AnotherChild { get; set; }
            public Guid Id { get; set; }
        }
    }

    public class RemoveValidator
    {
        [Fact]
        public void RemovessValidator()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            editor.RemoveValidator(validatorToRemove);

            editor.Connector.Validators.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsTrueIfValidatorWasRemoved()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var validatorToRemove = new MockValidator<object>();
            editor.AddValidator(validatorToRemove);

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfValidatorWasNotRemoved()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var validatorToRemove = new MockValidator<object>();

            var result = editor.RemoveValidator(validatorToRemove);

            result.Should().BeFalse();
        }
    }

    public class ResetToDefaults
    {
        [Fact]
        public void ResetsCrossSourcePoolingStrategy()
        {
            var design = CreateDesign<object>(crossSourceStrategy: PoolingStrategy.Queue, perSourceStrategy: PoolingStrategy.Aggregate);
            var editor = new ConnectorInputEditor<object>(design);
            editor.Connector.CrossSourcePoolingStrategy = PoolingStrategy.Aggregate;

            editor.ResetToDefaults();

            editor.Connector.CrossSourcePoolingStrategy.Should().Be(design.DefaultCrossSourcePoolingStrategy);
        }

        [Fact]
        public void ResetsPerSourcePoolingStrategy()
        {
            var design = CreateDesign<object>(crossSourceStrategy: PoolingStrategy.Queue, perSourceStrategy: PoolingStrategy.Aggregate);
            var editor = new ConnectorInputEditor<object>(design);
            editor.Connector.PerSourcePoolingStrategy = PoolingStrategy.Queue;

            editor.ResetToDefaults();

            editor.Connector.PerSourcePoolingStrategy.Should().Be(design.DefaultPerSourcePoolingStrategy);
        }

        [Fact]
        public void ResetsCrossSourceAggregatingPoolingToDefault()
        {
            var design = CreateDesign(crossSourcePooling: new DefaultPooling<object>());
            var editor = new ConnectorInputEditor<object>(design);
            editor.Connector.CrossSourceAggregatingPooling = new AnotherPooling<object>();

            editor.ResetToDefaults();

            editor.Connector.CrossSourceAggregatingPooling.Should().BeOfType(design.DefaultCrossSourceAggregatingPooling!.GetType());
        }

        [Fact]
        public void ResetsPerSourceAggregatingPoolingToDefault()
        {
            var design = CreateDesign(perSourcePooling: new DefaultPooling<object>());
            var editor = new ConnectorInputEditor<object>(design);
            editor.Connector.PerSourceAggregatingPooling = new AnotherPooling<object>();

            editor.ResetToDefaults();

            editor.Connector.PerSourceAggregatingPooling.Should().BeOfType(design.DefaultPerSourceAggregatingPooling!.GetType());
        }

        [Fact]
        public void ClonesCrossSourceAggregatingPooling()
        {
            var design = CreateDesign(crossSourcePooling: new DefaultPooling<object>());
            var editor = new ConnectorInputEditor<object>(design);

            editor.ResetToDefaults();

            editor.Connector.CrossSourceAggregatingPooling.Should().NotBeSameAs(design.DefaultCrossSourceAggregatingPooling);
        }

        [Fact]
        public void ClonesPerSourceAggregatingPooling()
        {
            var design = CreateDesign(perSourcePooling: new DefaultPooling<object>());
            var editor = new ConnectorInputEditor<object>(design);

            editor.ResetToDefaults();

            editor.Connector.PerSourceAggregatingPooling.Should().NotBeSameAs(design.DefaultPerSourceAggregatingPooling);
        }

        [Fact]
        public void SetsDefaultValue()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            editor.Connector.Value = new object();

            editor.ResetToDefaults();

            editor.Connector.Value.Should().BeSameAs(design.DefaultValue);
        }

        private sealed class DefaultPooling<TValue> : IAggregatingPooling<TValue>
        {
            public bool IsUserChangeable => throw new NotImplementedException();
            public string Description => throw new NotImplementedException();
            public string Name => throw new NotImplementedException();

            public TValue? Aggregate(IEnumerable<TValue?> values) => throw new NotImplementedException();
        }

        private sealed class AnotherPooling<TValue> : IAggregatingPooling<TValue>
        {
            public bool IsUserChangeable => throw new NotImplementedException();
            public string Description => throw new NotImplementedException();
            public string Name => throw new NotImplementedException();

            public TValue? Aggregate(IEnumerable<TValue?> values) => throw new NotImplementedException();
        }
    }

    public class SetMarkAsChangedOnlyIfNotEqual
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetsMarkAsChangedOnlyIfNotEqual(bool expectedValue)
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            editor.Connector.MarkAsChangedOnlyIfNotEqual = expectedValue == false;

            editor.SetMarkAsChangedOnlyIfNotEqual(expectedValue);

            editor.Connector.MarkAsChangedOnlyIfNotEqual.Should().Be(expectedValue);
        }
    }

    public class SetCrossSourceAggregatingPooling
    {
        [Fact]
        public void SetsAggregatingPooling()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var poolingToSet = Substitute.For<IAggregatingPooling<object>>();

            editor.SetCrossSourceAggregatingPooling(poolingToSet);

            editor.Connector.CrossSourceAggregatingPooling.Should().BeSameAs(poolingToSet);
        }

        [Fact]
        public void ThrowsIfSettingNullAndStrategyIsAggregate()
        {
            var design = CreateDesign<object>(crossSourceStrategy: PoolingStrategy.Aggregate);
            var editor = new ConnectorInputEditor<object>(design);

            Action action = () => editor.SetCrossSourceAggregatingPooling(null);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*pooling*not*null*");
        }

        [Fact]
        public void DoesNotThrowIfSettingNullAndStrategyIsNotAggregate()
        {
            var design = CreateDesign<object>(crossSourceStrategy: PoolingStrategy.Queue);
            var editor = new ConnectorInputEditor<object>(design);

            Action action = () => editor.SetCrossSourceAggregatingPooling(null);

            action.Should().NotThrow();
        }
    }

    public class SetCrossSourcePoolingStrategy
    {
        [Theory]
        [InlineData(PoolingStrategy.Aggregate)]
        [InlineData(PoolingStrategy.Concat)]
        [InlineData(PoolingStrategy.Queue)]
        public void SetsPoolingStrategy(PoolingStrategy strategy)
        {
            var design = CreateDesign<object[]>();
            var editor = new ConnectorInputEditor<object[]>(design);

            editor.SetCrossSourcePoolingStrategy(strategy, strategy == PoolingStrategy.Aggregate ? Substitute.For<IAggregatingPooling<object[]>>() : null);

            editor.Connector.CrossSourcePoolingStrategy.Should().Be(strategy);
        }

        [Fact]
        public void SetsDefaultAggregatingPooling()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var aggregatingPoolingToSet = Substitute.For<IAggregatingPooling<object>>();

            editor.SetCrossSourcePoolingStrategy(PoolingStrategy.Aggregate, aggregatingPoolingToSet);

            editor.Connector.CrossSourceAggregatingPooling.Should().BeSameAs(aggregatingPoolingToSet);
        }

        [Fact]
        public void ThrowsIfSettingConcatStrategyButTypeIsNoArray()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);

            Action action = () => editor.SetCrossSourcePoolingStrategy(PoolingStrategy.Concat);

            action.Should().Throw<ArgumentException>()
                .WithMessage($"*{PoolingStrategy.Concat}*must*array*");
        }
    }

    public class SetPerSourceAggregatingPooling
    {
        [Fact]
        public void SetsAggregatingPooling()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var poolingToSet = Substitute.For<IAggregatingPooling<object>>();

            editor.SetPerSourceAggregatingPooling(poolingToSet);

            editor.Connector.PerSourceAggregatingPooling.Should().BeSameAs(poolingToSet);
        }

        [Fact]
        public void ThrowsIfSettingNullAndStrategyIsAggregate()
        {
            var design = CreateDesign<object>(perSourceStrategy: PoolingStrategy.Aggregate);
            var editor = new ConnectorInputEditor<object>(design);

            Action action = () => editor.SetPerSourceAggregatingPooling(null);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*pooling*not*null*");
        }

        [Fact]
        public void DoesNotThrowIfSettingNullAndStrategyIsNotAggregate()
        {
            var design = CreateDesign<object>(perSourceStrategy: PoolingStrategy.Queue);
            var editor = new ConnectorInputEditor<object>(design);

            Action action = () => editor.SetPerSourceAggregatingPooling(null);

            action.Should().NotThrow();
        }
    }

    public class SetPerSourcePoolingStrategy
    {
        [Theory]
        [InlineData(PoolingStrategy.Aggregate)]
        [InlineData(PoolingStrategy.Concat)]
        [InlineData(PoolingStrategy.Queue)]
        public void SetsPoolingStrategy(PoolingStrategy strategy)
        {
            var design = CreateDesign<object[]>();
            var editor = new ConnectorInputEditor<object[]>(design);

            editor.SetPerSourcePoolingStrategy(strategy, strategy == PoolingStrategy.Aggregate ? Substitute.For<IAggregatingPooling<object[]>>() : null);

            editor.Connector.PerSourcePoolingStrategy.Should().Be(strategy);
        }

        [Fact]
        public void SetsDefaultAggregatingPooling()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);
            var aggregatingPoolingToSet = Substitute.For<IAggregatingPooling<object>>();

            editor.SetPerSourcePoolingStrategy(PoolingStrategy.Aggregate, aggregatingPoolingToSet);

            editor.Connector.PerSourceAggregatingPooling.Should().BeSameAs(aggregatingPoolingToSet);
        }

        [Fact]
        public void ThrowsIfSettingConcatStrategyButTypeIsNoArray()
        {
            var design = CreateDesign<object>();
            var editor = new ConnectorInputEditor<object>(design);

            Action action = () => editor.SetPerSourcePoolingStrategy(PoolingStrategy.Concat);

            action.Should().Throw<ArgumentException>()
                .WithMessage($"*{PoolingStrategy.Concat}*must*array*");
        }
    }

    public class SetValue
    {
        [Fact]
        public void SetsValue()
        {
            var design = CreateDesign<int>();
            var editor = new ConnectorInputEditor<int>(design);
            var valueToSet = 42;

            editor.SetValue(valueToSet);

            editor.Connector.Value.Should().Be(valueToSet);
        }

        [Fact]
        public void UsesReturnValueOfDesignValidator()
        {
            var design = CreateDesign<int>();
            var editor = new ConnectorInputEditor<int>(design);
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
            var design = CreateDesign<int>();
            var editor = new ConnectorInputEditor<int>(design);
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
            var design = CreateDesign<int>();
            var editor = new ConnectorInputEditor<int>(design);
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

    private static ConnectorDesignInput<TValue> CreateDesign<TValue>(PoolingStrategy crossSourceStrategy = PoolingStrategy.Queue, IAggregatingPooling<TValue>? crossSourcePooling = null,
        PoolingStrategy perSourceStrategy = PoolingStrategy.Queue, IAggregatingPooling<TValue>? perSourcePooling = null)
        => new()
        {
            DefaultCrossSourcePoolingStrategy = crossSourceStrategy,
            DefaultCrossSourceAggregatingPooling = crossSourcePooling ?? Substitute.For<IAggregatingPooling<TValue>>(),
            DefaultPerSourcePoolingStrategy = perSourceStrategy,
            DefaultPerSourceAggregatingPooling = perSourcePooling ?? Substitute.For<IAggregatingPooling<TValue>>(),
        };
}
