using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Engine;
using Xunit;

public class ExternalOutgoingRuntimeLinkTests
{
    public class Constructor
    {
        [Fact]
        public void MarksLinkAsChanged()
        {
            ExternalOutgoingRuntimeLink<object> link = new(CreateLink<object>(), default);

            link.Changed.Should().BeTrue();
        }

        private static IExternalOutgoingLink<TValue> CreateLink<TValue>()
        {
            var link = Substitute.For<IExternalOutgoingLink<TValue>>();
            link.TypeConversion.Returns((ITypeConversion?)null);
            return link;
        }
    }
}
