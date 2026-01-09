using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class RegexValidatorTests
{
    public class Validate
    {
        private const string ExactlyOneDigitPattern = @"^\d$";

        [Fact]
        public void ThrowsIfInputIsInvalidAndShouldThrow()
        {
            var validator = CreateValidator(ExactlyOneDigitPattern, default);

            var action = new Action(() => validator.Validate("666", true));

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ReturnsReplacementValueIfInputIsInvalidAndShouldNotThrow()
        {
            var expectedOutput = "7";
            var validator = CreateValidator(ExactlyOneDigitPattern, expectedOutput);

            var output = validator.Validate("666", false);

            output.Should().Be(expectedOutput);
        }

        [Fact]
        public void ReturnsInputIfInputIsValid()
        {
            var input = "4";
            var validator = CreateValidator(ExactlyOneDigitPattern, default);

            var output = validator.Validate(input, true);

            output.Should().Be(input);
        }

        private static RegexValidator CreateValidator(string pattern, string? replacementValue)
           => new()
           {
               Pattern = pattern,
               ReplacementValue = replacementValue,
           };
    }
}
