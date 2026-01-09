using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class ExternalOutgoingLinkEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesLinkIfNotSupplied()
        {
            var source = new ConnectorOutput<object>();
            var editor = new ExternalOutgoingLinkEditor<object>(source);

            editor.Link.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfLinkIsCreated()
        {
            var source = new ConnectorOutput<object>();
            var editor = new ExternalOutgoingLinkEditor<object>(source);

            editor.Link.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsSourceIfLinkIsCreated()
        {
            var source = new ConnectorOutput<object>();
            var editor = new ExternalOutgoingLinkEditor<object>(source);

            editor.Link.Source.Should().BeSameAs(source);
        }

        [Fact]
        public void StoresSuppliedLink()
        {
            var link = new ExternalOutgoingLink<object>();

            var editor = new ExternalOutgoingLinkEditor<object>(link);

            editor.Link.Should().BeSameAs(link);
        }
    }
}
