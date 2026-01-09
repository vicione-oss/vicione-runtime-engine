using AwesomeAssertions;
using System;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class InputConnectorDesignAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsIdToProperty()
        {
            var id = Guid.NewGuid();

            var attribute = new InputConnectorDesignAttribute(id.ToString());

            attribute.Id.Should().Be(id);
        }
    }
}
