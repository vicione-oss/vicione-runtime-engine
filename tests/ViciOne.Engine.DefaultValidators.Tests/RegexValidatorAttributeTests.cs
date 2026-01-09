using AwesomeAssertions;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class RegexValidatorAttributeTests
{
    public class Pattern
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var pattern = new RegexValidator().Pattern;
            var attribute = new RegexValidatorAttribute(pattern, default);

            attribute.Pattern.Should().Be(pattern);
        }
    }

    public class ReplacementValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var replacement = "7";
            var attribute = new RegexValidatorAttribute(new RegexValidator().Pattern, replacement);

            attribute.ReplacementValue.Should().Be(replacement);
        }
    }

    public class Validator
    {
        [Fact]
        public void ReturnsValidatorInstance()
        {
            var attribute = new RegexValidatorAttribute(new RegexValidator().Pattern, default);

            attribute.Validator.Should().BeOfType<RegexValidator>();
        }
    }
}
