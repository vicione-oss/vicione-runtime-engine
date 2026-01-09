using AwesomeAssertions;
using System;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class FunctionBlockDesignAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsIdToProperty()
        {
            var id = Guid.NewGuid();

            var attribute = new FunctionBlockDesignAttribute(id.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            attribute.Id.Should().Be(id);
        }

        [Fact]
        public void SetsFunctionBlockEnabledInputIdToProperty()
        {
            var functionBlockEnabledInputId = Guid.NewGuid();

            var attribute = new FunctionBlockDesignAttribute(Guid.NewGuid().ToString(), functionBlockEnabledInputId.ToString(), Guid.NewGuid().ToString());

            attribute.FunctionBlockEnabledInputId.Should().Be(functionBlockEnabledInputId);
        }

        [Fact]
        public void SetsFunctionBlockEnabledOutputIdToProperty()
        {
            var functionBlockEnabledOutputId = Guid.NewGuid();

            var attribute = new FunctionBlockDesignAttribute(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), functionBlockEnabledOutputId.ToString());

            attribute.FunctionBlockEnabledOutputId.Should().Be(functionBlockEnabledOutputId);
        }
    }
}
