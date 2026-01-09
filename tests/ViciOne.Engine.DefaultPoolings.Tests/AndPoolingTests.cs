using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class AndPoolingTests
{
    public class Aggregate
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { false }, false)]
        [InlineData(new[] { true }, true)]
        [InlineData(new[] { false, false }, false)]
        [InlineData(new[] { true, false }, false)]
        [InlineData(new[] { false, true }, false)]
        [InlineData(new[] { true, true }, true)]
        [InlineData(new[] { false, false, false }, false)]
        [InlineData(new[] { true, false, true }, false)]
        [InlineData(new[] { false, true, false }, false)]
        [InlineData(new[] { true, false, false }, false)]
        [InlineData(new[] { true, true, false }, false)]
        [InlineData(new[] { true, true, true }, true)]
        [InlineData(new[] { false, true, true }, false)]
        [InlineData(new[] { false, false, true }, false)]
        public void AggregatesCorrect(bool[] inputValues, bool expected)
        {
            var pooling = new AndPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
