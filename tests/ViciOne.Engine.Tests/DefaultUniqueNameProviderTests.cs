using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Engine;
using Xunit;

public class DefaultUniqueNameProviderTests
{

    public class GetUniqueName
    {
        [Fact]
        public void GetsCorrectUniqueNames()
        {
            var id = Guid.NewGuid();
            var expectedUniqueName = "fortytwo";
            var uniqueNames = new Dictionary<Guid, string>()
            {
                { id, expectedUniqueName }
            };
            var nameCache = new DefaultUniqueNameProvider(uniqueNames);
            var idProvider = Substitute.For<IIdProvider>();
            idProvider.Id.Returns(id);

            var uniqueName = nameCache.GetUniqueName(idProvider);

            uniqueName.Should().Be(expectedUniqueName);
        }

        [Fact]
        public void ReturnsUndefinedWhenIdNotFound()
        {
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());

            var uniqueName = nameCache.GetUniqueName(Substitute.For<IIdProvider>());

            uniqueName.Should().Be(DefaultUniqueNameProvider.UndefinedName);
        }
    }
}
