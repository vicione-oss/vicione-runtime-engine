using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class LinkChangedEntryFactoryTests
{
    public class Create
    {
        [Fact]
        public void CanCreateInstance()
        {
            var time = DateTime.Now;
            var id = Guid.NewGuid();
            var validity = Validity.Valid;
            var linkValue = 42;
            var expectedValue = 666;
            var link = Substitute.For<IRuntimeLink<int>>();
            link.ChangedTime.Returns(time);
            link.Link.Id.Returns(id);
            link.Validity.Returns(validity);
            link.Value.Returns(linkValue);
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();
            cloneHandler.CloneIfNecessary<int>(default).ReturnsForAnyArgs(expectedValue);
            using var factory = new LinkChangedEntryFactory();

            var entry = factory.Create(link, cloneHandler);

            entry.Should()
                .BeAssignableTo<ILinkChangedEntry<int>>().Which.Should()
                    .BeEquivalentTo(new
                    {
                        Time = time,
                        Id = id,
                        Validity = validity,
                        Value = expectedValue,
                    });
        }

        [Fact]
        public void ClonesValue()
        {
            var link = Substitute.For<IRuntimeLink<int>>();
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();
            using var factory = new LinkChangedEntryFactory();

            factory.Create(link, cloneHandler);

            cloneHandler.Received(1).CloneIfNecessary(Arg.Any<int>());
        }
    }
}
