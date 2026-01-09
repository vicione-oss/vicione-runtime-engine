using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class EqualityValidatorTests
{
    public class Validate
    {
        [Fact]
        public void ThrowsIfInputIsInvalidAndShouldThrow()
        {
            var validator = CreateValidator(42, default);

            Action action = () => validator.Validate(666, true);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ReturnsReplacementValueIfInputIsInvalidAndShouldNotThrow()
        {
            var expectedOutput = 333;
            var validator = CreateValidator(42, expectedOutput);

            var result = validator.Validate(666, false);

            result.Should().Be(expectedOutput);
        }

        [Fact]
        public void ReturnsInputIfInputIsValid()
        {
            var input = 42;
            var validator = CreateValidator(input, default);

            var result = validator.Validate(input, true);

            result.Should().Be(input);
        }

        private static EqualityValidator<int> CreateValidator(int validValue, int replacementValue)
           => new()
           {
               ValidValue = validValue,
               ReplacementValue = replacementValue,
           };
    }
}
