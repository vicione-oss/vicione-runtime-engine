using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using Xunit;

public class LinkExtensionsTests
{
    public class GetConnectedConnectors
    {
        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsConnectedConnectors))]
        public void GetsConnectedConnectors(ILink link, IConnector[] expectedConnectedConnectors)
        {
            var result = LinkExtensions.GetConnectedConnectors(link);

            result.Should().ContainAllButNotMore(expectedConnectedConnectors);
        }

        public static TheoryData<ILink, IConnector[]> ArgumentsFor_GetsConnectedConnectors
        {
            get
            {
                var source1 = Substitute.For<IConnector>();
                var source2 = Substitute.For<IConnector>();
                var destination1 = Substitute.For<IConnector>();
                var destination2 = Substitute.For<IConnector>();

                var linkWithSource = Substitute.For<ILinkWithSource>();
                linkWithSource.Source.Returns(source1);

                var linkWithDestination = Substitute.For<ILinkWithDestination>();
                linkWithDestination.Destination.Returns(destination1);

                var linkWithSourceAndDestination = Substitute.For<IInternalLink>();
                linkWithSourceAndDestination.Source.Returns(source2);
                linkWithSourceAndDestination.Destination.Returns(destination2);

                return new()
                {
                    { linkWithSource, [source1] },
                    { linkWithDestination, [destination1] },
                    { linkWithSourceAndDestination, [source2, destination2] },
                };
            }
        }
    }
}
