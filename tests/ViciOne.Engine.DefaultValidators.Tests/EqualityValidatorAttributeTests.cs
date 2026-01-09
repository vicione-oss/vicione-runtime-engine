using AwesomeAssertions;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class EqualityValidatorAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void InitializesValidatorWithCorrectSettings()
        {
            var valid = 4.2;
            var replacement = 66.6;

            var attribute = new EqualityValidatorAttribute<double>(valid, replacement);

            attribute.Validator.ValidValue.Should().Be(valid);
            attribute.Validator.ReplacementValue.Should().Be(replacement);
        }
    }

    public class ValidValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var valid = 4.2;
            var attribute = new EqualityValidatorAttribute<double>(valid, default);

            attribute.ValidValue.Should().Be(valid);
        }
    }

    public class ReplacementValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var replacement = 66.6;
            var attribute = new EqualityValidatorAttribute<double>(default, replacement);

            attribute.ReplacementValue.Should().Be(replacement);
        }
    }

    public class Validator
    {
        [Fact]
        public void RetunsValidatorInstance()
        {
            var attribute = new EqualityValidatorAttribute<double>(default, default);

            attribute.Validator.Should().BeOfType<EqualityValidator<double>>();
        }
    }
}
