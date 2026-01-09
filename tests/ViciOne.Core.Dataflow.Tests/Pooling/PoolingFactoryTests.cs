using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class PoolingFactoryTests
{
    public class GetAvailablePoolingTypes_Generic
    {
        [Fact]
        public void ReturnsAvailablePoolingTypes()
        {
            Type[] availableExpected = [typeof(DummyPooling1), typeof(DummyPooling2), typeof(DummyPooling5<bool>)];
            var factory = new PoolingFactory(GetAssemblies());

            var availableReturned = factory.GetAvailablePoolingTypes<bool>();

            // die Auflistung hat keine bestimmte Reihenfolge
            availableReturned.Should().ContainAllButNotMore(availableExpected);

        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Generische Überladung bevorzugen, wenn der Typ bekannt ist", Justification = "Hier soll explizit die nicht generische Überladung getestet werden")]
    public class GetAvailablePoolingTypes_NonGeneric
    {
        [Fact]
        public void ReturnsAvailablePoolingTypes()
        {
            Type[] availableExpected = [typeof(DummyPooling1), typeof(DummyPooling2), typeof(DummyPooling5<bool>)];
            var factory = new PoolingFactory(GetAssemblies());

            var availableReturned = factory.GetAvailablePoolingTypes(typeof(bool));

            // die Auflistung hat keine bestimmte Reihenfolge
            availableReturned.Should().ContainAllButNotMore(availableExpected);
        }
    }

    public class GetNewDefaultPooling_Generic
    {
        [Fact]
        public void ReturnsNewDefaultPoolingIfAvailable()
        {
            var expectedType = typeof(DummyPooling3);
            var factory = new PoolingFactory(GetAssemblies());

            var returned = factory.GetNewDefaultPooling<int>()!;

            returned.Should().BeOfType(expectedType);
        }

        [Fact]
        public void ReturnsNewDefaultPoolingIfMultipleAvailable()
        {
            Type[] expectedPossibleTypes = [typeof(DummyPooling1), typeof(DummyPooling2)];
            var factory = new PoolingFactory(GetAssemblies());

            var returned = factory.GetNewDefaultPooling<bool>()!;

            expectedPossibleTypes.Should().Contain(returned.GetType());
        }

        [Fact]
        public void ReturnsNullIfNoDefaultAvailable()
        {
            var factory = new PoolingFactory([]);

            var returned = factory.GetNewDefaultPooling<bool>();

            returned.Should().BeNull();
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Generische Überladung bevorzugen, wenn der Typ bekannt ist", Justification = "Hier soll explizit die nicht generische Überladung getestet werden")]
    public class GetNewDefaultPooling_NonGeneric
    {
        [Fact]
        public void ReturnsNewDefaultPoolingIfAvailable()
        {
            var expectedType = typeof(DummyPooling3);
            var factory = new PoolingFactory(GetAssemblies());

            var returned = factory.GetNewDefaultPooling(typeof(int))!;

            returned.Should().BeOfType(expectedType);
        }

        [Fact]
        public void ReturnsNewDefaultPoolingIfMultipleAvailable()
        {
            Type[] expectedPossibleTypes = [typeof(DummyPooling1), typeof(DummyPooling2)];
            var factory = new PoolingFactory(GetAssemblies());

            var returned = factory.GetNewDefaultPooling(typeof(bool))!;

            expectedPossibleTypes.Should().Contain(returned.GetType());
        }

        [Fact]
        public void ReturnsNullIfNoDefaultAvailable()
        {
            var factory = new PoolingFactory([]);

            var returned = factory.GetNewDefaultPooling(typeof(bool));

            returned.Should().BeNull();
        }
    }

    public class GetNewPooling_Generic
    {
        [Fact]
        public void ReturnsNewPoolingOnSuccess()
        {
            var expectedType = typeof(DummyPooling1);

            var returned = PoolingFactory.GetNewPooling<bool>(expectedType);

            returned.Should().BeOfType(expectedType);
        }
    }

    public class GetNewPooling_NonGeneric
    {
        [Fact]
        public void ReturnsNewPoolingOnSuccess()
        {
            var expectedType = typeof(DummyPooling1);

            var returned = PoolingFactory.GetNewPooling(expectedType);

            returned.Should().BeOfType(expectedType);
        }
    }

    private static Assembly[] GetAssemblies()
        => [.. System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.Assemblies];

    [DefaultPoolingForType<bool>]
    public class DummyPooling1 : IAggregatingPooling<bool>
    {
        public string Name => string.Empty;
        public string Description => string.Empty;
        public bool Aggregate(IEnumerable<bool> values) => throw new NotImplementedException();
    }

    [DefaultPoolingForType<bool>]
    public class DummyPooling2 : IAggregatingPooling<bool>
    {
        public string Name => string.Empty;
        public string Description => string.Empty;
        public bool Aggregate(IEnumerable<bool> values) => throw new NotImplementedException();
    }

    [DefaultPoolingForType<int>]
    public class DummyPooling3 : IAggregatingPooling<int>
    {
        public string Name => string.Empty;
        public string Description => string.Empty;
        public int Aggregate(IEnumerable<int> values) => throw new NotImplementedException();
    }

    public class DummyPooling4 : IAggregatingPooling<long>
    {
        public string Name => string.Empty;
        public string Description => string.Empty;
        public long Aggregate(IEnumerable<long> values) => throw new NotImplementedException();
    }

    public class DummyPooling5<T> : IAggregatingPooling<T>
    {
        public string Name => string.Empty;
        public string Description => string.Empty;
        public T? Aggregate(IEnumerable<T?> values) => throw new NotImplementedException();
    }
}
