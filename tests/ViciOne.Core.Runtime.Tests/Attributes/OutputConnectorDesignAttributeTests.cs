using AwesomeAssertions;
using System;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class OutputConnectorDesignAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsIdToProperty()
        {
            var id = Guid.NewGuid();

            var attribute = new OutputConnectorDesignAttribute(id.ToString());

            attribute.Id.Should().Be(id);
        }
    }
}
