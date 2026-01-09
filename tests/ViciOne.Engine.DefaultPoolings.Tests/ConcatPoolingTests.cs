using AwesomeAssertions;
using ViciOne.Engine.DefaultPoolings;
using Xunit;

public class ConcatPoolingTests
{
    public class Aggregate
    {
        [Theory(DisplayName = "")]
        [InlineData(new[] { "Eins" }, "Eins")]
        [InlineData(new[] { "Zwei" }, "Zwei")]
        [InlineData(new[] { "Eins", "Zwei" }, "EinsZwei")]
        [InlineData(new[] { "Drei", "Vier" }, "DreiVier")]
        [InlineData(new[] { "Drei", null }, "Drei")]
        [InlineData(new[] { "Eins", "Zwei", "Drei" }, "EinsZweiDrei")]
        [InlineData(new[] { "Vier", "Fünf", "Sechs" }, "VierFünfSechs")]
        [InlineData(new[] { "Vier", null, "Sechs" }, "VierSechs")]
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
