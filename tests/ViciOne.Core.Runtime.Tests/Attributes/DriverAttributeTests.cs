using AwesomeAssertions;
using System;
using System.Reflection;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class DriverAttributeTests
{
    public class Constructor
    {
        [Fact]
        public void StoresStringAsTypeName()
        {
            var typeName = "System.Int32";

            var attribute = new DriverAttribute(typeName);

            attribute.TypeName.Should().Be(typeName);
        }

        [Fact]
        public void StoresFullNameAsTypeName()
        {
            var type = typeof(int);

            var attribute = new TestDriverAttribute(type);

            attribute.TypeName.Should().Be(type.FullName);
        }

        [Fact]
        public void ThrowsIfTypeDoesNotProvideFullName()
        {
            var type = typeof(GenericType<>).GetTypeInfo().GenericTypeParameters[0];

            Action action = () => _ = new TestDriverAttribute(type);

            action.Should().Throw<ArgumentException>().WithMessage("*no*full*name*");
        }

        private sealed class TestDriverAttribute(Type type) : DriverAttribute(type) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Nicht instanziierte interne Klassen vermeiden.", Justification = "Der Typ wird als Typargument verwendet.")]
        private sealed class GenericType<T> { }
    }
}
