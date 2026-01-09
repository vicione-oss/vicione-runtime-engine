using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class XnorPoolingTests
{
    public class Aggregate
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { false }, true)]
        [InlineData(new[] { true }, false)]
        [InlineData(new[] { false, false }, true)]
        [InlineData(new[] { true, false }, false)]
        [InlineData(new[] { false, true }, false)]
        [InlineData(new[] { true, true }, true)]
        [InlineData(new[] { false, false, false }, true)]
        [InlineData(new[] { true, false, true }, true)]
        [InlineData(new[] { false, true, false }, false)]
        [InlineData(new[] { true, false, false }, false)]
        [InlineData(new[] { true, true, false }, true)]
        [InlineData(new[] { true, true, true }, false)]
        [InlineData(new[] { false, true, true }, true)]
        [InlineData(new[] { false, false, true }, false)]
        public void AggregatesCorrect(bool[] inputValues, bool expected)
        {
            var pooling = new XnorPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
