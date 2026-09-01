using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class ExceptionFactoryTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "The class represents the name of the method under test.")]
    public class CreateNoValidNamespaceOrClassNameException
    {
        [Fact]
        public void CreatesNoValidNamespaceOrClassNameException()
        {
            var result = ExceptionFactory.CreateNoValidNamespaceOrClassNameException(string.Empty);

            result.Should().NotBeNull();
        }

        [Fact]
        public void UsesSpecifiedNameInMessage()
        {
            var name = "Zwei und vierzig";

            var result = ExceptionFactory.CreateNoValidNamespaceOrClassNameException(name);

            result.Message.Should().Contain(name);
        }
    }
}
