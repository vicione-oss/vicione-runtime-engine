using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class DriverAttribute_T_Tests
{
    public class Constructor
    {
        [Fact]
        public void PassesTypeToBase()
        {
            var attribute = new DriverAttribute<DummyDriver>();

            attribute.TypeName.Should().Be(typeof(DummyDriver).FullName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "The type is used as a type argument.")]
        private sealed class DummyDriver : RuntimeDriver<DummyDriver>
        {
            public new ILogger Logger => base.Logger;
        }
    }
}
