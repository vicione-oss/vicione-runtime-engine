using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime;
using Xunit;

public class RuntimePersistentVariableTests
{
    public class Constructor
    {
        [Fact]
        public void UsesChangedTimeFromVariable()
        {
            var variable = Substitute.For<IPersistentVariable<int>>();
            var time = new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            variable.ChangedTime.Returns(time);
            ((IPersistable)variable).ChangedTime.Returns(time);

            var runtimeVariable = new RuntimePersistentVariable<int>(variable);

            runtimeVariable.ChangedTime.Should().Be(time);
        }

        [Fact]
        public void UsesValueFromVariable()
        {
            var variable = Substitute.For<IPersistentVariable<int>>();
            variable.Value.Returns(42);
            ((IPersistable<int>)variable).Value.Returns(42);

            var runtimeVariable = new RuntimePersistentVariable<int>(variable);

            runtimeVariable.Value.Should().Be(42);
        }
    }

    public class ApplyLoadedValue
    {
        [Fact]
        public void AppliesValue()
        {
            var runtimeVariable = CreateVariable<int>(default);
            object valueToSet = 42;

            runtimeVariable.ApplyLoadedValue(valueToSet, default);

            runtimeVariable.Value.Should().Be((int)valueToSet);
        }

        [Fact]
        public void AppliesChangedTime()
        {
            var runtimeVariable = CreateVariable<object>(default);
            var changedTimeToSet = DateTime.UtcNow;

            runtimeVariable.ApplyLoadedValue(default, changedTimeToSet);

            runtimeVariable.ChangedTime.Should().Be(changedTimeToSet);
        }

        [Fact]
        public void SetsChangedFlag()
        {
            var runtimeVariable = CreateVariable<object>(default);

            runtimeVariable.ApplyLoadedValue(default, default);

            runtimeVariable.Changed.Should().BeTrue();
        }

        [Fact]
        public void RaisesVariableChanged()
        {
            var runtimeVariable = CreateVariable<object>(default);
            var raisedVariableChanged = false;
            runtimeVariable.VariableChanged += (_) => raisedVariableChanged = true;

            runtimeVariable.ApplyLoadedValue(default, default);

            raisedVariableChanged.Should().BeTrue();
        }
    }

    public class SetValueIfNotEqual
    {
        [Fact]
        public void ReturnsFalseWhenSettingSameValue()
        {
            var runtimeVariable = CreateVariable(1);

            var changed = runtimeVariable.SetValueIfNotEqual(1);

            changed.Should().BeFalse();
        }

        [Fact]
        public void ReturnsTrueWhenSettingOtherValue()
        {
            var runtimeVariable = CreateVariable(1);

            var changed = runtimeVariable.SetValueIfNotEqual(2);

            changed.Should().BeTrue();
        }

        [Fact]
        public void SetsValue()
        {
            var runtimeVariable = CreateVariable(1);

            runtimeVariable.SetValueIfNotEqual(2);

            runtimeVariable.Value.Should().Be(2);
        }

        [Fact]
        public void DoesNotSetChangedTimeWhenSettingSameValue()
        {
            var runtimeVariable = CreateVariable(1);
            var changedTimeBefore = runtimeVariable.ChangedTime;

            runtimeVariable.SetValueIfNotEqual(1);

            runtimeVariable.ChangedTime.Should().Be(changedTimeBefore);
        }

        [Fact]
        public void SetsChangedTimeWhenSettingOtherValue()
        {
            var runtimeVariable = CreateVariable(1);
            var changedTimeBefore = runtimeVariable.ChangedTime;
            var now = DateTime.UtcNow;

            runtimeVariable.SetValueIfNotEqual(2);

            runtimeVariable.ChangedTime.Should().BeAfter(changedTimeBefore);
            runtimeVariable.ChangedTime.Should().BeOnOrAfter(now);
        }

        [Fact]
        public void DoesNotSetChangedFlagWhenSettingSameValue()
        {
            var runtimeVariable = CreateVariable(1);

            runtimeVariable.SetValueIfNotEqual(1);

            runtimeVariable.Changed.Should().BeFalse();
        }

        [Fact]
        public void SetsChangedFlagWhenSettingOtherValue()
        {
            var runtimeSetting = CreateVariable(1);

            runtimeSetting.SetValueIfNotEqual(2);

            runtimeSetting.Changed.Should().BeTrue();
        }
    }

    private static RuntimePersistentVariable<T> CreateVariable<T>(T? initialValue)
    {
        var variable = Substitute.For<IPersistentVariable<T>>();
        variable.Value.Returns(initialValue);
        ((IPersistentVariable)variable).Value.Returns(initialValue);
        ((IPersistable<T>)variable).Value.Returns(initialValue);
        return new RuntimePersistentVariable<T>(variable);
    }
}
