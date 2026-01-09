using AwesomeAssertions;
using NSubstitute;
using System;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using Xunit;

public class RuntimeSettingTests
{
    public class Constructor
    {
        [Fact]
        public void UsesChangedTimeFromSetting()
        {
            var setting = Substitute.For<ISetting<int>>();
            var time = new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            setting.ChangedTime.Returns(time);
            ((IPersistable)setting).ChangedTime.Returns(time);

            var runtimeSetting = new RuntimeSetting<int>(setting, default, default);

            runtimeSetting.ChangedTime.Should().Be(time);
        }

        [Fact]
        public void UsesValueFromSetting()
        {
            var setting = Substitute.For<ISetting<int>>();
            setting.Value.Returns(42);
            ((IPersistable<int>)setting).Value.Returns(42);

            var runtimeSetting = new RuntimeSetting<int>(setting, default, default);

            runtimeSetting.Value.Should().Be(42);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StoresConnectedValueForInput(bool inputConnected)
        {
            var setting = Substitute.For<ISetting<int>>();

            var runtimeSetting = new RuntimeSetting<int>(setting, inputConnected, default);

            runtimeSetting.Input.Connected.Should().Be(inputConnected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StoresConnectedValueForOutput(bool outputConnected)
        {
            var setting = Substitute.For<ISetting<int>>();

            var runtimeSetting = new RuntimeSetting<int>(setting, default, outputConnected);

            runtimeSetting.Output.Connected.Should().Be(outputConnected);
        }
    }

    public class ApplyLoadedValue
    {
        [Fact]
        public void AppliesValue()
        {
            var runtimeSetting = CreateSetting<int>(default);
            object valueToSet = 42;

            runtimeSetting.ApplyLoadedValue(valueToSet, default);

            runtimeSetting.Value.Should().Be((int)valueToSet);
        }

        [Fact]
        public void AppliesChangedTime()
        {
            var runtimeSetting = CreateSetting<object>(default);
            var changedTimeToSet = DateTime.UtcNow;

            runtimeSetting.ApplyLoadedValue(default, changedTimeToSet);

            runtimeSetting.ChangedTime.Should().Be(changedTimeToSet);
        }

        [Fact]
        public void SetsChangedFlag()
        {
            var runtimeSetting = CreateSetting<object>(default);

            runtimeSetting.ApplyLoadedValue(default, default);

            runtimeSetting.Changed.Should().BeTrue();
        }

        [Fact]
        public void RaisesSettingChanged()
        {
            var runtimeSetting = CreateSetting<object>(default);
            var raisedSettingChanged = false;
            runtimeSetting.SettingChanged += (_) => raisedSettingChanged = true;

            runtimeSetting.ApplyLoadedValue(default, default);

            raisedSettingChanged.Should().BeTrue();
        }

        [Fact]
        public void UsesReturnValueOfValidators()
        {
            var runtimeSetting = CreateSetting<int>(default);
            var validator = (DummyValidator<int>)runtimeSetting.Setting.Design.Validators.First();
            validator.SubstituteValue = 666;
            validator.UseSubstituteValue = true;

            runtimeSetting.ApplyLoadedValue(42, default);

            runtimeSetting.Value.Should().Be(validator.SubstituteValue);
        }
    }

    public class GetAndResetChanged
    {
        [Fact]
        public void GetsChangedFlag()
        {
            var runtimeSetting = CreateSetting<object>(default);
            runtimeSetting.MarkAsChanged();

            var changed = runtimeSetting.GetAndResetChanged();

            changed.Should().BeTrue();
        }

        [Fact]
        public void ResetsChangedFlag()
        {
            var runtimeSetting = CreateSetting<object>(default);
            runtimeSetting.MarkAsChanged();

            runtimeSetting.GetAndResetChanged();

            runtimeSetting.Changed.Should().BeFalse();
        }
    }

    public class MarkAsChanged
    {
        [Fact]
        public void InvokesSettingChanged()
        {
            var runtimeSetting = CreateSetting<object>(default);
            var handler = Substitute.For<Action<IRuntimeSetting>>();
            runtimeSetting.SettingChanged += handler;

            runtimeSetting.MarkAsChanged();

            runtimeSetting.Changed.Should().BeTrue();
            handler.Received().Invoke(runtimeSetting);
        }

        [Fact]
        public void SetsChangedFlag()
        {
            var runtimeSetting = CreateSetting<object>(default);

            runtimeSetting.MarkAsChanged();

            runtimeSetting.Changed.Should().BeTrue();
        }
    }

    public class Process
    {
        [Fact]
        public void SetsValueIfInputChangedAndValid()
        {
            var runtimeSetting = CreateSetting(1);
            runtimeSetting.Input.SetValueIfNotEqual(2);
            runtimeSetting.Input.SetValidityIfNotEqual(Validity.Valid);
            var cycleInfo = Substitute.For<IFunctionBlockCycleInfo>();

            runtimeSetting.Process(cycleInfo);

            runtimeSetting.Value.Should().Be(2);
            runtimeSetting.Changed.Should().BeTrue();
        }

        [Theory]
        [InlineData(true, Validity.Invalid)]
        [InlineData(false, Validity.Valid)]
        public void DoesNotSetValueIfInputNotChangedOrNotValid(bool changed, Validity validity)
        {
            var runtimeSetting = CreateSetting(1, validity);
            runtimeSetting.Input.SetValueIfNotEqual(2);
            runtimeSetting.Input.GetAndResetChanged();
            if (changed)
                runtimeSetting.Input.MarkAsChanged();
            var cycleInfo = Substitute.For<IFunctionBlockCycleInfo>();

            runtimeSetting.Process(cycleInfo);

            runtimeSetting.Value.Should().Be(1);
            runtimeSetting.Changed.Should().BeFalse();
        }
    }

    public class SetValueIfNotEqual
    {
        [Fact]
        public void ReturnsFalseWhenSettingSameValue()
        {
            var runtimeSetting = CreateSetting(1);

            var changed = runtimeSetting.SetValueIfNotEqual(1);

            changed.Should().BeFalse();
        }

        [Fact]
        public void ReturnsTrueWhenSettingOtherValue()
        {
            var runtimeSetting = CreateSetting(1);

            var changed = runtimeSetting.SetValueIfNotEqual(2);

            changed.Should().BeTrue();
        }

        [Fact]
        public void SetsValue()
        {
            var runtimeSetting = CreateSetting(1);

            runtimeSetting.SetValueIfNotEqual(2);

            runtimeSetting.Value.Should().Be(2);
        }

        [Fact]
        public void DoesNotSetChangedTimeWhenSettingSameValue()
        {
            var runtimeSetting = CreateSetting(1);
            var changedTimeBefore = runtimeSetting.ChangedTime;

            runtimeSetting.SetValueIfNotEqual(1);

            runtimeSetting.ChangedTime.Should().Be(changedTimeBefore);
        }

        [Fact]
        public void SetsChangedTimeWhenSettingOtherValue()
        {
            var runtimeSetting = CreateSetting(1);
            var changedTimeBefore = runtimeSetting.ChangedTime;
            var now = DateTime.UtcNow;

            runtimeSetting.SetValueIfNotEqual(2);

            runtimeSetting.ChangedTime.Should().BeAfter(changedTimeBefore);
            runtimeSetting.ChangedTime.Should().BeOnOrAfter(now);
        }

        [Fact]
        public void DoesNotSetChangedFlagWhenSettingSameValue()
        {
            var runtimeSetting = CreateSetting(1);

            runtimeSetting.SetValueIfNotEqual(1);

            runtimeSetting.Changed.Should().BeFalse();
        }

        [Fact]
        public void SetsChangedFlagWhenSettingOtherValue()
        {
            var runtimeSetting = CreateSetting(1);

            runtimeSetting.SetValueIfNotEqual(2);

            runtimeSetting.Changed.Should().BeTrue();
        }

        [Fact]
        public void UsesReturnValueOfValidators()
        {
            var runtimeSetting = CreateSetting<int>(default);
            var validator = (DummyValidator<int>)runtimeSetting.Setting.Design.Validators.First();
            validator.SubstituteValue = 666;
            validator.UseSubstituteValue = true;

            runtimeSetting.SetValueIfNotEqual(42);

            runtimeSetting.Value.Should().Be(validator.SubstituteValue);
        }
    }

    private static RuntimeSetting<T> CreateSetting<T>(T? initialValue, Validity initialValidity = Validity.Valid)
    {
        var setting = Substitute.For<ISetting<T>>();
        setting.Value.Returns(initialValue);
        ((ISetting)setting).Value.Returns(initialValue);
        ((IPersistable<T>)setting).Value.Returns(initialValue);
        var input = Substitute.For<IConnector<T>>();
        var output = Substitute.For<IConnector<T>>();
        input.Design.Validators.Returns([new DummyValidator<T>()]);
        output.Design.Validators.Returns([new DummyValidator<T>()]);
        input.Validators.Returns([new DummyValidator<T>()]);
        output.Validators.Returns([new DummyValidator<T>()]);
        setting.Input.Returns(input);
        setting.Output.Returns(output);
        ((ISetting)setting).Input.Returns(input);
        ((ISetting)setting).Output.Returns(output);
        setting.Design.Validators.Returns([new DummyValidator<T>()]);
        var runtimeSetting = new RuntimeSetting<T>(setting, default, default);
        runtimeSetting.Input.SetValidityIfNotEqual(initialValidity);
        runtimeSetting.Input.GetAndResetChanged();
        return runtimeSetting;
    }

    private sealed class DummyValidator<T> : IValidator<T>
    {
        public T? SubstituteValue { get; set; }
        public bool UseSubstituteValue { get; set; }

        public T? Validate(T? value, bool throwIfInvalid)
            => UseSubstituteValue ? SubstituteValue : value;
        object? IValidator.Validate(object? value, bool throwIfInvalid)
            => Validate((T?)value, throwIfInvalid);
    }
}
