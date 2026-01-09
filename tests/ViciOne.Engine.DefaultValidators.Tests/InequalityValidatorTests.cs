using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class InequalityValidatorTests
{
    public class Validate
    {
        [Fact]
        public void ThrowsIfInputIsInvalidAndShouldThrow()
        {
            var validator = CreateValidator(42, default);

            Action action = () => validator.Validate(42, true);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ReturnsReplacementValueIfInputIsInvalidAndShouldNotThrow()
        {
            var invalid = 42;
            var expectedOutput = 333;
            var validator = CreateValidator(invalid, expectedOutput);

            var result = validator.Validate(invalid, false);

            result.Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData(566437507)]
        [InlineData(1960170224)]
        [InlineData(979376727)]
        [InlineData(18078510)]
        [InlineData(404449150)]
        [InlineData(-1985513718)]
        [InlineData(-627807134)]
        [InlineData(-56808762)]
        [InlineData(-1131343638)]
        [InlineData(-637988008)]
        public void ReturnsInputIfInputIsValid(int input)
        {
            var validator = CreateValidator(42, default);

            var result = validator.Validate(input, true);

            result.Should().Be(input);
        }

        private static InequalityValidator<int> CreateValidator(int invalidValue, int replacementValue)
           => new()
           {
               InvalidValue = invalidValue,
               ReplacementValue = replacementValue,
           };
    }
}
