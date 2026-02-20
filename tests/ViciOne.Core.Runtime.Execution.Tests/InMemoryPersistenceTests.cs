using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class InMemoryPersistenceTests
{
    public class Dispose
    {
        [Fact]
        public void CanDispose()
        {
            using var persistence = new InMemoryPersistence();

            Action action = persistence.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            using var persistence = new InMemoryPersistence();

            Action action = () =>
            {
                persistence.Dispose();
                persistence.Dispose();
            };

            action.Should().NotThrow();
        }
    }

    public class GetAndResetPersisted
    {
        [Fact]
        public void GetsPersistedEntries()
        {
            using var persistence = new InMemoryPersistence();
            var persistedEntries = CreateChangedEntries();
            persistence.Persist(persistedEntries);

            var returned = persistence.GetAndResetPersisted();

            returned.Should().ContainAllButNotMore(persistedEntries);
        }

        [Fact]
        public void ResetsPersistedEntries()
        {
            using var persistence = new InMemoryPersistence();
            var persistedEntries = CreateChangedEntries();
            persistence.Persist(persistedEntries);
            _ = persistence.GetAndResetPersisted();

            var returned = persistence.GetAndResetPersisted();

            returned.Should().BeEmpty();
        }
    }

    public class GetPersisted
    {
        [Fact]
        public void GetsPersistedEntries()
        {
            using var persistence = new InMemoryPersistence();
            var persistedEntries = CreateChangedEntries();
            persistence.Persist(persistedEntries);

            var returned = persistence.GetPersisted();

            returned.Should().ContainAllButNotMore(persistedEntries);
        }
    }

    public class Persist
    {
        [Fact]
        public void StoresSpecifiedEntries()
        {
            using var persistence = new InMemoryPersistence();
            var entriesToPersist = CreateChangedEntries();

            persistence.Persist(entriesToPersist);

            persistence.GetPersisted().Should().ContainAllButNotMore(entriesToPersist);
        }

        [Fact]
        public void RaisesEntriesAdded()
        {
            using var persistence = new InMemoryPersistence();
            var entriesToPersist = CreateChangedEntries();
            using var monitoredManager = persistence.Monitor();

            persistence.Persist(entriesToPersist);

            monitoredManager.Should()
                .Raise(nameof(InMemoryPersistence.EntriesAdded))
                .WithSender(persistence);
        }

        [Fact]
        public void DoesNothingIfNoEntriesSpecified()
        {
            using var persistence = new InMemoryPersistence();
            using var monitoredManager = persistence.Monitor();

            persistence.Persist([]);

            persistence.GetPersisted().Should().BeEmpty();
            monitoredManager.Should().NotRaise(nameof(InMemoryPersistence.EntriesAdded));
        }
    }

    public class RaiseEntriesLoaded
    {
        [Fact]
        public void RaisesEntriesLoaded()
        {
            using var persistence = new InMemoryPersistence();
            var loadedEntries = CreateChangedEntries();
            using var monitoredManager = persistence.Monitor();

            persistence.RaiseEntriesLoaded(loadedEntries);

            monitoredManager.Should()
                .Raise(nameof(InMemoryPersistence.EntriesLoaded))
                .WithArgs<IEnumerable<IPersistenceEntry>>(args => args.SequenceEqual(loadedEntries));
        }
    }

    public class RequestAllValues
    {
        [Fact]
        public void RaisesAllValuesToPersistRequested()
        {
            using var persistence = new InMemoryPersistence();
            // HACK: persistence.Monitor() throws an InvalidProgramException
            var allValuesToPersistRequestedRaised = false;
            persistence.AllValuesToPersistRequested += () =>
            {
                allValuesToPersistRequestedRaised = true;
                return [];
            };

            persistence.RequestAllValues();

            allValuesToPersistRequestedRaised.Should().BeTrue();
        }

        [Fact]
        public void ReturnsEntriesReturnedByEventHandler()
        {
            using var persistence = new InMemoryPersistence();
            var expectedEntries = CreateChangedEntries();
            persistence.AllValuesToPersistRequested += () => expectedEntries;

            var returned = persistence.RequestAllValues();

            returned.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public void ReturnsEmptyEnumerationIfNoEventHandlerAttached()
        {
            using var persistence = new InMemoryPersistence();

            var returned = persistence.RequestAllValues();

            returned.Should().BeEmpty();
        }
    }

    private static ChangedEntry[] CreateChangedEntries()
        =>
        [
            new() { Time = DateTime.UtcNow.AddSeconds(1), Id = Guid.Parse("cd9d8207-f956-4be2-8bf4-5c2d26c2924b"), Validity = Validity.Valid, Value = 42, },
            new() { Time = DateTime.UtcNow.AddSeconds(2), Id = Guid.Parse("4f8472a2-2e31-4b92-bb9b-cb8679e0ce0e"), Validity = Validity.Valid, Value = 66.6, },
            new() { Time = DateTime.UtcNow.AddSeconds(3), Id = Guid.Parse("652dd67c-0b8e-4f93-9e05-0a55d3a31608"), Validity = Validity.Invalid, Value = "Test", },
            new() { Time = DateTime.UtcNow.AddSeconds(4), Id = Guid.Parse("fc273d45-f239-493c-98ea-d5e28a5d0707"), Validity = Validity.Invalid, Value = true, },
        ];
}
