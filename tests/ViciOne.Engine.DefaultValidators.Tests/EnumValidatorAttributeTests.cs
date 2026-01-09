using AwesomeAssertions;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class EnumValidatorAttributeTests
{
    public class ReplacementValue
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var replacement = MockEnum.Second;
            var attribute = new EnumValidatorAttribute<MockEnum>(replacement);

            attribute.ReplacementValue.Should().Be(replacement);
        }
    }

    public class Validator
    {
        [Fact]
        public void ReturnsValidatorInstance()
        {
            var attribute = new EnumValidatorAttribute<MockEnum>(default);

            attribute.Validator.Should().BeOfType<EnumValidator<MockEnum>>();
        }
    }

    private enum MockEnum
    {
        First,
        Second,
    }
}
