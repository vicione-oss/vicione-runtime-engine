using AwesomeAssertions;
using ViciOne.Core.Dataflow;
using Xunit;

public class NameValidatorTests
{
    public class IsValidNamespaceOrClassName
    {
        [Theory]
        [InlineData("N")]
        [InlineData("Name")]
        [InlineData("NAME")]
        [InlineData("_Name")]
        [InlineData("Na:me")]
        [InlineData("Na::me")]
        [InlineData("Name.Name")]
        [InlineData("Name[]")]
        [InlineData("Name`Name")]
        [InlineData("Náme")]
        [InlineData("Nâme")]
        [InlineData("Name<T>")]
        public void ReturnsTrueForValidNamespaceOrClassName(string namespaceOrClassName)
        {
            var valid = NameValidator.IsValidNamespaceOrClassName(namespaceOrClassName);

            valid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData("2Name")]
        [InlineData("Name. Name")]
        [InlineData("Name´Name")]
        [InlineData("Name=Name")]
        [InlineData("N^me")]
        public void ReturnsFalseForInvalidNamespaceOrClassName(string namespaceOrClassName)
        {
            var valid = NameValidator.IsValidNamespaceOrClassName(namespaceOrClassName);

            valid.Should().BeFalse();
        }
    }
}
