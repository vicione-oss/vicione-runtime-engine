using AwesomeAssertions;
using System;
using System.Reflection;
using ViciOne.Engine;
using Xunit;

public class GenericFactoryTests
{
    public class Constructor
    {
        [Fact]
        public void ThrowsIfGenericCreateMethodIsNotStatic()
        {
            var genericCreateMethod = typeof(GenericFactoryTests).GetMethod(nameof(CreateA_NotStatic), BindingFlags.NonPublic | BindingFlags.Instance)!;

            Action action = () => _ = new GenericFactory(genericCreateMethod);

            action.Should()
                .Throw<ArgumentException>()
                .WithParameterName("genericCreateMethod");
        }
    }

    public class Create
    {
        [Fact]
        public void CanCreateInstance()
        {
            var genericCreateMethod = typeof(GenericFactoryTests).GetMethod(nameof(CreateA), BindingFlags.NonPublic | BindingFlags.Static)!;
            var value = 42;
            using var factory = new GenericFactory(genericCreateMethod);

            var instance = factory.Create<IA>(value.GetType(), [value]);

            instance.Should()
                .BeAssignableTo<A<int>>().Which
                    .Value.Should().Be(value);
        }
    }

    public class Dispose
    {
        [Fact]
        public void CanDispose()
        {
            var genericCreateMethod = typeof(GenericFactoryTests).GetMethod(nameof(CreateA), BindingFlags.NonPublic | BindingFlags.Static)!;
            using var factory = new GenericFactory(genericCreateMethod);

            Action action = factory.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var genericCreateMethod = typeof(GenericFactoryTests).GetMethod(nameof(CreateA), BindingFlags.NonPublic | BindingFlags.Static)!;
            using var factory = new GenericFactory(genericCreateMethod);
            factory.Dispose();

            Action action = factory.Dispose;

            action.Should().NotThrow();
        }
    }

    private static A<T> CreateA<T>(T value) where T : notnull
        => new(value);

#pragma warning disable CA1822 // Mark members as static
    private A<T> CreateA_NotStatic<T>(T value) where T : notnull
#pragma warning restore CA1822 // Mark members as static
        => new(value);

    private interface IA
    {
        object Value { get; }
    }

    private sealed class A<T>(T value) : IA where T : notnull
    {
        public T Value { get; set; } = value;
        object IA.Value => Value;
    }
}
