using AwesomeAssertions;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class StringLengthValidatorAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void InitializesValidatorWithCorrectLimits()
        {
            uint min = 42;
            uint max = 666;

            var attribute = new StringLengthValidatorAttribute(min, max);

            attribute.Validator.MinLength.Should().Be(min);
            attribute.Validator.MaxLength.Should().Be(max);
        }
    }

    public class MaxLength
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            uint min = 42;
            uint max = 666;
            var attribute = new StringLengthValidatorAttribute(min, max);

            attribute.MaxLength.Should().Be(max);
        }
    }

    public class MinLength
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            uint min = 42;
            uint max = 666;
            var attribute = new StringLengthValidatorAttribute(min, max);

            attribute.MinLength.Should().Be(min);
        }
    }
}
