using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class EnumValidatorTests
{
    public class Validate
    {
        [Fact]
        public void ThrowsIfInputIsInvalidAndShouldThrow()
        {
            var validator = CreateValidator(default);

            var action = new Action(() => validator.Validate((MockEnum)666, true));

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ReturnsReplacementValueIfInputIsInvalidAndShouldNotThrow()
        {
            var expectedOutput = MockEnum.Second;
            var validator = CreateValidator(expectedOutput);

            var output = validator.Validate((MockEnum)666, false);

            output.Should().Be(expectedOutput);
        }

        [Fact]
        public void ReturnsInputIfInputIsValid()
        {
            var input = MockEnum.First;
            var validator = CreateValidator(default);

            var output = validator.Validate(input, true);

            output.Should().Be(input);
        }

        private static EnumValidator<MockEnum> CreateValidator(MockEnum replacementValue)
           => new()
           {
               ReplacementValue = replacementValue,
           };

        private enum MockEnum
        {
            First,
            Second,
        }
    }
}
