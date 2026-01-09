using AwesomeAssertions;
using NSubstitute;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Engine;
using Xunit;

public class DefaultPoolingValidatorTests
{
    public class IsLinkValid
    {
        [Theory]
        [InlineData(Validity.Valid)]
        [InlineData(Validity.Invalid)]
        public void ReturnsLinkValid(Validity expectedValidity)
        {
            var link = Substitute.For<IRuntimeLink<object>>();
            link.Validity.Returns(expectedValidity);
            var validator = new DefaultPoolingValidator();

            var returnedLinkValidity = validator.IsLinkValid(link)
                ? Validity.Valid
                : Validity.Invalid;

            returnedLinkValidity.Should().Be(expectedValidity);
        }
    }

    public class GetResultValidity
    {
        public static TheoryData<IRuntimeLink<object>[], Validity> ArgumentsFor_ReturnsCorrectValue => new()
        {
            { [CreateLink(Validity.Invalid), CreateLink(Validity.Invalid)], Validity.Invalid },
            { [CreateLink(Validity.Valid), CreateLink(Validity.Invalid)], Validity.Invalid },
            { [CreateLink(Validity.Valid), CreateLink(Validity.Valid)], Validity.Valid },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ReturnsCorrectValue))]
        public void ReturnsCorrectValue(IRuntimeLink<object>[] links, Validity expected)
        {
            var validator = new DefaultPoolingValidator();

            var returned = validator.GetResultValidity(links);

            returned.Should().Be(expected);
        }

        [Fact]
        public void ReturnsDefaultValidityIfNoLinksProvided()
        {
            var validator = new DefaultPoolingValidator();

            var returned = validator.GetResultValidity<object>([]);

            returned.Should().Be(RuntimeConnectorDefaults.DefaultValidity);
        }
    }

    public class SelectLinks
    {
        public static TheoryData<IEnumerable<IRuntimeLink<object>>, IEnumerable<IRuntimeLink<object>>> ArgumentsFor_SelectsCorrectLinks
        {
            get
            {
                var link11 = CreateLink(Validity.Invalid);
                var link12 = CreateLink(Validity.Invalid);
                var link13 = CreateLink(Validity.Invalid);

                var link21 = CreateLink(Validity.Valid);
                var link22 = CreateLink(Validity.Invalid);
                var link23 = CreateLink(Validity.Invalid);

                var link31 = CreateLink(Validity.Valid);
                var link32 = CreateLink(Validity.Valid);
                var link33 = CreateLink(Validity.Invalid);

                var link41 = CreateLink(Validity.Valid);
                var link42 = CreateLink(Validity.Valid);
                var link43 = CreateLink(Validity.Valid);

                return new()
                {
                    { [link11, link12, link13], [] },
                    { [link21, link22, link23], [link21] },
                    { [link31, link32, link33], [link31, link32] },
                    { [link41, link42, link43], [link41, link42, link43] },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_SelectsCorrectLinks))]
        public void SelectsCorrectLinks(IEnumerable<IRuntimeLink<object>> links, IEnumerable<IRuntimeLink<object>> expected)
        {
            var validator = new DefaultPoolingValidator();

            var returned = validator.SelectLinks(links);

            returned.Should().BeEquivalentTo(expected);
        }
    }

    private static IRuntimeLink<object> CreateLink(Validity validity)
    {
        var link = Substitute.For<IRuntimeLink<object>>();
        link.Validity.Returns(validity);
        return link;
    }
}
