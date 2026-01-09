using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class StringLengthValidatorTests
{
    public class Validate
    {
        [Theory]
        [InlineData("AAAAAA", 0, 5)]
        [InlineData("AAAAAA", 8, 10)]
        public void ThrowsIfInputLengthIsInvalidAndShouldThrow(string input, uint minLength, uint maxLength)
        {
            var validator = CreateValidator(minLength, maxLength);

            Action action = () => validator.Validate(input, true);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData("AAAAAA", 0, 5, "AAAAA")]
        [InlineData("AAAAAA", 8, 10, "AAAAAA  ")]
        public void ReturnsCorrectedInputIfInputLengthIsInvalidAndShouldNotThrow(string input, uint minLength, uint maxLength, string expectedOutput)
        {
            var validator = CreateValidator(minLength, maxLength);

            var result = validator.Validate(input, false);

            result.Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData("AAAAAA", 0, 6)]
        [InlineData("AAAAAA", 6, 10)]
        [InlineData("AAAAAA", 0, 10)]
        public void ReturnsInputIfInputLengthIsValid(string input, uint minLength, uint maxLength)
        {
            var validator = CreateValidator(minLength, maxLength);

            var result = validator.Validate(input, true);

            result.Should().Be(input);
        }

        [Fact]
        public void ThrowsIfInputIsNullAndNullIsForbiddenAndShouldThrow()
        {
            var validator = CreateValidator(10, 100);

            Action action = () => validator.Validate(null, true);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ReturnsNullIfInputIsNullAndNullIsForbiddenAndShouldNotThrow()
        {
            var validator = CreateValidator(10, 100);

            var result = validator.Validate(null, false);

            result.Should().BeNull();
        }

        [Fact]
        public void ReturnsNullIfInputIsNullAndNullIsAllowed()
        {
            var validator = CreateValidator(0, 100);

            var result = validator.Validate(null, true);

            result.Should().BeNull();
        }

        private static StringLengthValidator CreateValidator(uint minLength, uint maxLength)
            => new()
            {
                MinLength = minLength,
                MaxLength = maxLength
            };
    }
}
