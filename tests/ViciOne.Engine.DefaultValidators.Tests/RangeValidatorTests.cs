using AwesomeAssertions;
using System;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class RangeValidatorTests
{
    public class Validate_Double
    {
        [Theory]
        [InlineData(6, 0, 5)]
        [InlineData(6, 8, 10)]
        [InlineData(-6, 0, 5)]
        [InlineData(-6, -5, -1)]
        [InlineData(-6, -10, -8)]
        public void ThrowsIfInputIsOutOfRangeAndShouldThrow(double input, double minValue, double maxValue)
        {
            var validator = CreateValidator(minValue, maxValue);

            Action action = () => validator.Validate(input, true);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(6, 0, 5, 5)]
        [InlineData(6, 8, 10, 8)]
        [InlineData(-6, 0, 5, 0)]
        [InlineData(-6, -5, -1, -5)]
        [InlineData(-6, -10, -8, -8)]
        public void ReturnsCorrectedInputIfInputIsOutOfRangeAndShouldNotThrow(double input, double minValue, double maxValue, double expectedOutput)
        {
            var validator = CreateValidator(minValue, maxValue);

            var result = validator.Validate(input, false);

            result.Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData(6, 0, 6)]
        [InlineData(6, 6, 10)]
        [InlineData(6, 0, 10)]
        [InlineData(-6, -6, -1)]
        [InlineData(-6, -10, -6)]
        [InlineData(-6, -10, 0)]
        public void ReturnsInputIfInputIsInRange(double input, double minValue, double maxValue)
        {
            var validator = CreateValidator(minValue, maxValue);

            var result = validator.Validate(input, true);

            result.Should().Be(input);
        }

        private static RangeValidator<double> CreateValidator(double minValue, double maxValue)
           => new()
           {
               MinValue = minValue,
               MaxValue = maxValue
           };
    }

    public class Validate_Long
    {
        [Theory]
        [InlineData(6, 0, 5)]
        [InlineData(6, 8, 10)]
        [InlineData(-6, 0, 5)]
        [InlineData(-6, -5, -1)]
        [InlineData(-6, -10, -8)]
        public void ThrowsIfInputIsOutOfRange(long input, long minValue, long maxValue)
        {
            var validator = CreateValidator(minValue, maxValue);

            Action action = () => validator.Validate(input, true);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(6, 0, 5, 5)]
        [InlineData(6, 8, 10, 8)]
        [InlineData(-6, 0, 5, 0)]
        [InlineData(-6, -5, -1, -5)]
        [InlineData(-6, -10, -8, -8)]
        public void ReturnsCorrectedInputIfInputIsOutOfRangeAndShouldNotThrow(long input, long minValue, long maxValue, long expectedOutput)
        {
            var validator = CreateValidator(minValue, maxValue);

            var result = validator.Validate(input, false);

            result.Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData(6, 0, 6)]
        [InlineData(6, 6, 10)]
        [InlineData(6, 0, 10)]
        [InlineData(-6, -6, -1)]
        [InlineData(-6, -10, -6)]
        [InlineData(-6, -10, 0)]
        public void ReturnsInputIfInputIsInRange(long input, long minValue, long maxValue)
        {
            var validator = CreateValidator(minValue, maxValue);

            var result = validator.Validate(input, true);

            result.Should().Be(input);
        }

        private static RangeValidator<long> CreateValidator(long minValue, long maxValue)
           => new()
           {
               MinValue = minValue,
               MaxValue = maxValue
           };
    }

    public class Validate_UserDefined
    {
        public static TheoryData<UserDefined, UserDefined, UserDefined> ArgumentsFor_ThrowsIfInputIsOutOfRange => new()
        {
            { new UserDefined(6), new UserDefined(0), new UserDefined(5) },
            { new UserDefined(6), new UserDefined(8), new UserDefined(10) },
            { new UserDefined(-6), new UserDefined(0), new UserDefined(5) },
            { new UserDefined(-6), new UserDefined(-5), new UserDefined(-1) },
            { new UserDefined(-6), new UserDefined(-10), new UserDefined(-8) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ThrowsIfInputIsOutOfRange))]
        public void ThrowsIfInputIsOutOfRangeAndShouldThrow(UserDefined input, UserDefined minValue, UserDefined maxValue)
        {
            var validator = CreateValidator(minValue, maxValue);

            Action action = () => validator.Validate(input, true);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        public static TheoryData<UserDefined, UserDefined, UserDefined, UserDefined> ArgumentsFor_ReturnsCorrectedInputIfInputIsOutOfRangeAndShouldNotThrow => new()
        {
            { new UserDefined(6), new UserDefined(0), new UserDefined(5), new UserDefined(5) },
            { new UserDefined(6), new UserDefined(8), new UserDefined(10), new UserDefined(8) },
            { new UserDefined(-6), new UserDefined(0), new UserDefined(5), new UserDefined(0) },
            { new UserDefined(-6), new UserDefined(-5), new UserDefined(-1), new UserDefined(-5) },
            { new UserDefined(-6), new UserDefined(-10), new UserDefined(-8), new UserDefined(-8) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ReturnsCorrectedInputIfInputIsOutOfRangeAndShouldNotThrow))]
        public void ReturnsCorrectedInputIfInputIsOutOfRangeAndShouldNotThrow(UserDefined input, UserDefined minValue, UserDefined maxValue, UserDefined expectedOutput)
        {
            var validator = CreateValidator(minValue, maxValue);

            var result = validator.Validate(input, false);

            result.Should().BeEquivalentTo(expectedOutput);
        }

        public static TheoryData<UserDefined, UserDefined, UserDefined> ArgumentsFor_ReturnsInputIfInputIsInRange => new()
        {
            { new UserDefined(6), new UserDefined(0), new UserDefined(6) },
            { new UserDefined(6), new UserDefined(6), new UserDefined(10) },
            { new UserDefined(6), new UserDefined(0), new UserDefined(10) },
            { new UserDefined(-6), new UserDefined(-6), new UserDefined(-1) },
            { new UserDefined(-6), new UserDefined(-10), new UserDefined(-6) },
            { new UserDefined(-6), new UserDefined(-10), new UserDefined(0) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ReturnsInputIfInputIsInRange))]
        public void ReturnsInputIfInputIsInRange(UserDefined input, UserDefined minValue, UserDefined maxValue)
        {
            var validator = CreateValidator(minValue, maxValue);

            var result = validator.Validate(input, true);

            result.Should().Be(input);
        }

        [Fact]
        public void ReturnsNullIfInputIsNull()
        {
            var validator = CreateValidator(null, null);

            var result = validator.Validate(null, true);

            result.Should().BeNull();
        }

        private static RangeValidator<UserDefined> CreateValidator(UserDefined? minValue, UserDefined? maxValue)
          => new()
          {
              MinValue = minValue,
              MaxValue = maxValue
          };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "RangeValidator only uses the CompareTo method.")]
        public class UserDefined(int value) : IComparable<UserDefined?>
        {
            public int Value { get; set; } = value;

            public int CompareTo(UserDefined? other)
                => Value.CompareTo(other?.Value);
        }
    }
}
