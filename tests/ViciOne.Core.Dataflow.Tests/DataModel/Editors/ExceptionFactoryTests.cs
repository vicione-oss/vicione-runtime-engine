using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class ExceptionFactoryTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Bezeichner dürfen kein falsches Suffix aufweisen", Justification = "Die Klasse stellt den zu testenden Methodennamen dar.")]
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
