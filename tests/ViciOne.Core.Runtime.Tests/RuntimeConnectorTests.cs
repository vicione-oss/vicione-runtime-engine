using AwesomeAssertions;
using NSubstitute;
using System;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime;
using Xunit;

public class RuntimeConnectorTests
{
    public class Constructor
    {
        [Fact]
        public void UsesValueFromConnector()
        {
            var connector = Substitute.For<IConnector<object>>();
            connector.Value.Returns(42);

            var runtimeConnector = new RuntimeConnector<object>(connector, default);

            runtimeConnector.Value.Should().Be(42);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StoresConnectedValue(bool connected)
        {
            var connector = Substitute.For<IConnector<object>>();

            var runtimeConnector = new RuntimeConnector<object>(connector, connected);

            runtimeConnector.Connected.Should().Be(connected);
        }
    }

    public class GetAndResetChanged
    {
        [Fact]
        public void GetsChangedFlag()
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnector<object>(connector, default);
            runtimeConnector.MarkAsChanged();

            var changed = runtimeConnector.GetAndResetChanged();

            changed.Should().BeTrue();
        }

        [Fact]
        public void ResetsChangedFlag()
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnector<object>(connector, default);
            runtimeConnector.MarkAsChanged();

            runtimeConnector.GetAndResetChanged();

            runtimeConnector.Changed.Should().BeFalse();
        }
    }

    public class MarkAsChanged
    {
        [Fact]
        public void SetsChangedFlag()
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnector<object>(connector, default);

            runtimeConnector.MarkAsChanged();

            runtimeConnector.Changed.Should().BeTrue();
        }

        [Fact]
        public void SetsChangedTime()
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnector<object>(connector, default);
            var changedTimeBefore = runtimeConnector.ChangedTime;
            var timeBefore = DateTime.UtcNow;

            runtimeConnector.MarkAsChanged();

            runtimeConnector.ChangedTime.Should().BeAfter(changedTimeBefore);
            runtimeConnector.ChangedTime.Should().BeOnOrAfter(timeBefore)
                .And.BeOnOrBefore(DateTime.UtcNow);
        }
    }

    public class SetValidityIfNotEqual
    {
        [Theory]
        [InlineData(Validity.Invalid)]
        [InlineData(Validity.Valid)]
        public void ReturnsFalseWhenSettingSameValue(Validity validityBefore)
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnectorWithInitialValidValue<object>(connector, validityBefore);
            var validityToSet = validityBefore;

            var result = runtimeConnector.SetValidityIfNotEqual(validityToSet);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(Validity.Invalid)]
        [InlineData(Validity.Valid)]
        public void ReturnsTrueWhenSettingOtherValue(Validity validityBefore)
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnectorWithInitialValidValue<object>(connector, validityBefore);
            var validityToSet = validityBefore == Validity.Invalid
                ? Validity.Valid
                : Validity.Invalid;

            var result = runtimeConnector.SetValidityIfNotEqual(validityToSet);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(Validity.Invalid)]
        [InlineData(Validity.Valid)]
        public void SetsValidity(Validity validityBefore)
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnectorWithInitialValidValue<object>(connector, validityBefore);
            var validityToSet = validityBefore == Validity.Invalid
                ? Validity.Valid
                : Validity.Invalid;

            runtimeConnector.SetValidityIfNotEqual(validityToSet);

            runtimeConnector.Validity.Should().Be(validityToSet);
        }

        [Theory]
        [InlineData(Validity.Invalid)]
        [InlineData(Validity.Valid)]
        public void DoesNotSetChangedFlagWhenSettingSameValue(Validity validityBefore)
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnectorWithInitialValidValue<object>(connector, validityBefore);
            var validToSet = validityBefore;

            runtimeConnector.SetValidityIfNotEqual(validToSet);

            runtimeConnector.Changed.Should().BeFalse();
        }

        [Theory]
        [InlineData(Validity.Invalid)]
        [InlineData(Validity.Valid)]
        public void SetsChangedFlagWhenSettingOtherValue(Validity validityBefore)
        {
            var connector = Substitute.For<IConnector<object>>();
            var runtimeConnector = new RuntimeConnectorWithInitialValidValue<object>(connector, validityBefore);
            var validityToSet = validityBefore == Validity.Invalid
                ? Validity.Valid
                : Validity.Invalid;

            runtimeConnector.SetValidityIfNotEqual(validityToSet);

            runtimeConnector.Changed.Should().BeTrue();
        }

        private sealed class RuntimeConnectorWithInitialValidValue<TValue> : RuntimeConnector<TValue>
        {
            public RuntimeConnectorWithInitialValidValue(IConnector<TValue> connector, Validity initialValidity) : base(connector, default)
                => Validity = initialValidity;
        }
    }

    public class SetValueIfNotEqual
    {
        [Fact]
        public void ReturnsFalseWhenSettingSameValueAndMarkAsChangedOnlyIfNotEqualIsTrue()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.MarkAsChangedOnlyIfNotEqual.Returns(true);
            connector.Value.Returns(42);
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 42;

            var result = runtimeConnector.SetValueIfNotEqual(valueToSet);

            result.Should().BeFalse();
        }

        [Fact]
        public void ReturnsTrueWhenSettingSameValueAndMarkAsChangedOnlyIfNotEqualIsFalse()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.Value.Returns(42);
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 42;

            var result = runtimeConnector.SetValueIfNotEqual(valueToSet);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsTrueWhenSettingOtherValue()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.Value.Returns(42);
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 666;

            var result = runtimeConnector.SetValueIfNotEqual(valueToSet);

            result.Should().BeTrue();
        }

        [Fact]
        public void SetsValue()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.Value.Returns(42);
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 666;

            runtimeConnector.SetValueIfNotEqual(valueToSet);

            runtimeConnector.Value.Should().Be(valueToSet);
        }

        [Fact]
        public void DoesNotSetChangedFlagWhenSettingSameValue()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.MarkAsChangedOnlyIfNotEqual.Returns(true);
            connector.Value.Returns(42);
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 42;

            runtimeConnector.SetValueIfNotEqual(valueToSet);

            runtimeConnector.Changed.Should().BeFalse();
        }

        [Fact]
        public void SetsChangedFlagWhenSettingOtherValue()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.Value.Returns(42);
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 666;

            runtimeConnector.SetValueIfNotEqual(valueToSet);

            runtimeConnector.Changed.Should().BeTrue();
        }

        [Fact]
        public void UsesReturnValueOfValidatorsOfDesign()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.Design.Validators.Returns([new MockValidator<int>()]);
            var validator = (MockValidator<int>)connector.Design.Validators.First();
            validator.SubstituteValue = 666;
            validator.UseSubstituteValue = true;
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 42;

            runtimeConnector.SetValueIfNotEqual(valueToSet);

            runtimeConnector.Value.Should().Be(validator.SubstituteValue);
        }

        [Fact]
        public void UsesReturnValueOfValidatorsOfConnector()
        {
            var connector = Substitute.For<IConnector<int>>();
            connector.Validators.Returns([new MockValidator<int>()]);
            var validator = (MockValidator<int>)connector.Validators.First();
            validator.SubstituteValue = 666;
            validator.UseSubstituteValue = true;
            var runtimeConnector = new RuntimeConnector<int>(connector, default);
            var valueToSet = 42;

            runtimeConnector.SetValueIfNotEqual(valueToSet);

            runtimeConnector.Value.Should().Be(validator.SubstituteValue);
        }
    }
}
