using AwesomeAssertions;
using System;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class SettingDesignAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void SetsIdToProperty()
        {
            var id = Guid.NewGuid();

            var attribute = new SettingDesignAttribute(id.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            attribute.Id.Should().Be(id);
        }

        [Fact]
        public void SetsEnabledInputIdToProperty()
        {
            var inputId = Guid.NewGuid();

            var attribute = new SettingDesignAttribute(Guid.NewGuid().ToString(), inputId.ToString(), Guid.NewGuid().ToString());

            attribute.InputId.Should().Be(inputId);
        }

        [Fact]
        public void SetsEnabledOutputIdToProperty()
        {
            var outputId = Guid.NewGuid();

            var attribute = new SettingDesignAttribute(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), outputId.ToString());

            attribute.OutputId.Should().Be(outputId);
        }
    }
}
