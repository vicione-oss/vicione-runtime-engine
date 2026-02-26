using AwesomeAssertions;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Runtime.Execution;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class RelevantAssembliesTests
{
    public class AllAssemblies
    {
        [Fact]
        public void ReturnsAdditionalAssembliesAndFbAssembly()
        {
            var expectedFbAssembly = GetType().Assembly;
            Assembly[] expectedAdditionalAssemblies =
            [
                typeof(AndPooling).Assembly,
                typeof(RangeValidator<>).Assembly,
            ];
            var created = new RelevantAssemblies(expectedFbAssembly, expectedAdditionalAssemblies);

            created.AllAssemblies.Should()
                .Contain(expectedFbAssembly).And
                .Contain(expectedAdditionalAssemblies);
        }
    }

    public class Constructor
    {
        [Fact]
        public void StoresFbAssembly()
        {
            var expectedFbAssembly = GetType().Assembly;

            var created = new RelevantAssemblies(expectedFbAssembly, Enumerable.Empty<Assembly>());

            created.FbAssembly.Should().BeSameAs(expectedFbAssembly);
        }

        [Fact]
        public void StoresAdditionalAssemblies()
        {
            Assembly[] expectedAdditionalAssemblies =
            [
                typeof(AndPooling).Assembly,
                typeof(RangeValidator<>).Assembly,
            ];

            var created = new RelevantAssemblies(GetType().Assembly, expectedAdditionalAssemblies);

            created.AdditionalAssemblies.Should().BeSameAs(expectedAdditionalAssemblies);
        }
    }
}
