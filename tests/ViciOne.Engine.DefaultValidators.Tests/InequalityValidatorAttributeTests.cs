using AwesomeAssertions;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class InequalityValidatorAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void InitializesValidatorWithCorrectSettings()
        {
            var invalid = 4.2;
            var replacement = 66.6;

            var attribute = new InequalityValidatorAttribute<double>(invalid, replacement);

            attribute.Validator.InvalidValue.Should().Be(invalid);
            attribute.Validator.ReplacementValue.Should().Be(replacement);
        }
    }

    public class InvalidValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var invalid = 4.2;
            var attribute = new InequalityValidatorAttribute<double>(invalid, default);

            attribute.InvalidValue.Should().Be(invalid);
        }
    }

    public class ReplacementValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var replacement = 66.6;
            var attribute = new InequalityValidatorAttribute<double>(default, replacement);

            attribute.ReplacementValue.Should().Be(replacement);
        }
    }

    public class Validator
    {
        [Fact]
        public void RetunsValidatorInstance()
        {
            var attribute = new InequalityValidatorAttribute<double>(default, default);

            attribute.Validator.Should().BeOfType<InequalityValidator<double>>();
        }
    }
}
