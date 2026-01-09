using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class OrPoolingTests
{
    public class Aggregate
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { false }, false)]
        [InlineData(new[] { true }, true)]
        [InlineData(new[] { false, false }, false)]
        [InlineData(new[] { true, false }, true)]
        [InlineData(new[] { false, true }, true)]
        [InlineData(new[] { true, true }, true)]
        [InlineData(new[] { false, false, false }, false)]
        [InlineData(new[] { true, false, true }, true)]
        [InlineData(new[] { false, true, false }, true)]
        [InlineData(new[] { true, false, false }, true)]
        [InlineData(new[] { true, true, false }, true)]
        [InlineData(new[] { true, true, true }, true)]
        [InlineData(new[] { false, true, true }, true)]
        [InlineData(new[] { false, false, true }, true)]
        public void AggregatesCorrect(bool[] inputValues, bool expected)
        {
            var pooling = new OrPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
