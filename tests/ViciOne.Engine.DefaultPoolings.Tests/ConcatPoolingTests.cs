using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class ConcatPoolingTests
{
    public class Aggregate
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { "One" }, "One")]
        [InlineData(new[] { "Two" }, "Two")]
        [InlineData(new[] { "One", "Two" }, "OneTwo")]
        [InlineData(new[] { "Three", "Four" }, "ThreeFour")]
        [InlineData(new[] { "Three", null }, "Three")]
        [InlineData(new[] { "One", "Two", "Three" }, "OneTwoThree")]
        [InlineData(new[] { "Four", "Five", "Six" }, "FourFiveSix")]
        [InlineData(new[] { "Four", null, "Six" }, "FourSix")]
        [InlineData(new string?[] { null }, "")]
        [InlineData(new string?[] { null, null }, "")]
        [InlineData(new string?[] { null, null, null }, "")]
        public void AggregatesCorrect(string[] inputValues, string expected)
        {
            var pooling = new ConcatPooling();

            var result = pooling.Aggregate(inputValues);

            result.Should().Be(expected);
        }
    }
}
