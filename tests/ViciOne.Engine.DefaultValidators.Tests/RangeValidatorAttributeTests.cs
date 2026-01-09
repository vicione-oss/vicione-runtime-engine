using AwesomeAssertions;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class RangeValidatorAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void InitializesValidatorWithCorrectLimits()
        {
            long min = 42;
            long max = 666;

            var attribute = new RangeValidatorAttribute<long>(min, max);

            attribute.Validator.MinValue.Should().Be(min);
            attribute.Validator.MaxValue.Should().Be(max);
        }
    }

    public class MaxValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            long min = 42;
            long max = 666;
            var attribute = new RangeValidatorAttribute<long>(min, max);

            attribute.MaxValue.Should().Be(max);
        }
    }

    public class MinValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            long min = 42;
            long max = 666;
            var attribute = new RangeValidatorAttribute<long>(min, max);

            attribute.MinValue.Should().Be(min);
        }
    }

    public class Validator
    {
        [Fact]
        public void RetunsValidatorInstance()
        {
            var attribute = new RangeValidatorAttribute<long>(default, default);

            attribute.Validator.Should().BeOfType<RangeValidator<long>>();
        }
    }
}
