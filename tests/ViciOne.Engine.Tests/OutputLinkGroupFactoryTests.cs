using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class OutputLinkGroupFactoryTests
{
    public class Create
    {
        [Fact]
        public void CanCreateInstance()
        {
            var connector = Substitute.For<IRuntimeConnector<int>>();
            IRuntimeLink<int>[] links =
            [
                Substitute.For<IRuntimeLink<int>>(),
                Substitute.For<IRuntimeLink<int>>(),
            ];
            using var factory = new OutputLinkGroupFactory();

            var entry = factory.Create(connector, links);

            entry.Should().NotBeNull();
            entry.Connector.Should().BeSameAs(connector);
            entry.Links.Should().BeEquivalentTo(links);
        }
    }
}
