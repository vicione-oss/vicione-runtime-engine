using AwesomeAssertions;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class ChangedEntryFactoryTests
{
    public class Create
    {
        [Fact]
        public void CreatesFromLinkChangedEntry()
        {
            MockLinkChangedEntry sourceEntry = new()
            {
                Time = DateTime.Now,
                Id = Guid.NewGuid(),
                Validity = Validity.Valid,
                Value = new object(),
            };

            var changedEntry = ChangedEntryFactory.Create(sourceEntry);

            changedEntry.Time.Should().Be(sourceEntry.Time);
            changedEntry.Id.Should().Be(sourceEntry.Id);
            changedEntry.Validity.Should().Be(sourceEntry.Validity);
            changedEntry.Value.Should().Be(sourceEntry.Value);
        }

        [Fact]
        public void CreatesFromPersistenceEntry()
        {
            MockPersistenceEntry sourceEntry = new()
            {
                Time = DateTime.Now,
                Id = Guid.NewGuid(),
                Value = new object(),
            };

            var changedEntry = ChangedEntryFactory.Create(sourceEntry);

            changedEntry.Time.Should().Be(sourceEntry.Time);
            changedEntry.Id.Should().Be(sourceEntry.Id);
            changedEntry.Validity.Should().Be(Validity.Valid);
            changedEntry.Value.Should().Be(sourceEntry.Value);
        }

        private class MockChangedEntry
        {
            public Guid Id { get; set; }
            public DateTime Time { get; set; }
            public Validity Validity { get; set; }
            public object? Value { get; set; }
        }

        private class MockLinkChangedEntry : MockChangedEntry, ILinkChangedEntry { }
        private class MockPersistenceEntry : MockChangedEntry, IPersistenceEntry { }
    }
}
