using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable correctly", Justification = "The class that tests the Dispose method is meant to be named Dispose")]
public abstract partial class PersistentVariablesPersistenceProcessorTests : IDisposable
{
    private protected IEnumerable<IRuntimeFunctionBlock> AllBlocks { get; }
    private protected IEnumerable<IRuntimePersistentVariable> AllPersistentVariables { get; }
    private protected virtual Action? ApplyLoadedValueCallback { get; }
    private protected TestLogger Logger { get; } = new();
    private protected MockPersistence Persistence { get; } = Substitute.ForPartsOf<MockPersistence>();
    private protected PersistentVariablesPersistenceProcessor PersistenceProcessor { get; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The created FBs are assigned to the AllBlocks property and are disposed through it as well.")]
    protected PersistentVariablesPersistenceProcessorTests()
    {
        IRuntimeFunctionBlock[] fbs =
        [
            CreateBlockWithPersistentVariables(1, ApplyLoadedValueCallback),
            CreateBlockWithPersistentVariables(0, ApplyLoadedValueCallback),
            CreateBlockWithPersistentVariables(2, ApplyLoadedValueCallback),
        ];
        IReferenceTypeCloneHandler cloneHandler = new MockCloneHandler();
        IReferenceTypeCloneHandler GetCloneHandler() => cloneHandler;
        var dataflow = Substitute.For<IDataflow>();
        dataflow.RootContainer.FunctionBlocks.Returns(fbs.Select(x => x.FunctionBlock));
        var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
        var nameCache = new DefaultUniqueNameProvider(uniqueNames);

        PersistenceProcessor = new PersistentVariablesPersistenceProcessor(fbs, nameCache, Persistence, GetCloneHandler, Logger);
        AllBlocks = fbs;
        AllPersistentVariables = fbs.SelectMany(x => x.PersistentVariables);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not relevant for the tests")]
    void IDisposable.Dispose()
    {
        foreach (var block in AllBlocks)
            block.Dispose();
        PersistenceProcessor.Dispose();
        GC.SuppressFinalize(this);
    }

    public partial class Constructor : PersistentVariablesPersistenceProcessorTests
    {
        [Fact]
        public void CanHandleUnknownPersistentVariablesWhenLoaded()
        {
            var entries = new List<IPersistenceEntry>();
            var notExistingEntry = Substitute.For<IPersistenceEntry<object>>();
            var notExistingGuid = Guid.NewGuid();
            notExistingEntry.Id.Returns(notExistingGuid);
            entries.Add(notExistingEntry);
            var existingEntry = Substitute.For<IPersistenceEntry<int>>();
            var existingGuid = AllPersistentVariables.First().Variable.Id;
            existingEntry.Id.Returns(existingGuid);
            entries.Add(existingEntry);

            Persistence.EntriesLoaded += Raise.Event<Action<IEnumerable<IPersistenceEntry>>>(entries);

            var loggedMessages = Logger.GetLoggedMessages();
            loggedMessages.Should()
                .ContainSingle(x => x.Contains(notExistingGuid.ToString(), StringComparison.OrdinalIgnoreCase)).And
                .NotContain(x => x.Contains(existingGuid.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ReturnsAllPersistentVariablesValuesToPersistenceWhenRequested()
        {
            var persistentVariablesValues = Persistence.RequestAllValues();

            persistentVariablesValues.Should().HaveSameCount(AllPersistentVariables);
            foreach (var persistentVariable in AllPersistentVariables)
            {
                persistentVariablesValues.Should()
                    .ContainSingle(x => x.Id == persistentVariable.Variable.Id)
                        .Which.Time.Should().Be(persistentVariable.ChangedTime);
            }
        }
    }

    public class ApplyLoadedPersistentVariables : PersistentVariablesPersistenceProcessorTests
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Sufficient for the functionality to be tested here")]
        private protected override Action ApplyLoadedValueCallback => () =>
        {
            if (ThrowOnApplyValue)
                throw new Exception();
        };
        private bool ThrowOnApplyValue { get; set; }

        [Fact]
        public void CallsApplyLoadedValue()
        {
            var entriesToLoad = CreateEntriesToLoad();
            Persistence.EntriesLoaded += Raise.Event<Action<IEnumerable<IPersistenceEntry>>>(entriesToLoad.Values);

            PersistenceProcessor.ApplyLoaded();

            foreach (var persistentVariable in AllPersistentVariables)
            {
                var entryToLoad = entriesToLoad[persistentVariable.Variable.Id];
                persistentVariable.Received().ApplyLoadedValue(entryToLoad.Value, entryToLoad.Time);
            }
        }

        [Fact]
        public void AppliesOnlyOnce()
        {
            var entriesToLoad = CreateEntriesToLoad();
            Persistence.EntriesLoaded += Raise.Event<Action<IEnumerable<IPersistenceEntry>>>(entriesToLoad.Values);

            PersistenceProcessor.ApplyLoaded();
            PersistenceProcessor.ApplyLoaded();

            foreach (var persistentVariable in AllPersistentVariables)
            {
                var entryToLoad = entriesToLoad[persistentVariable.Variable.Id];
                persistentVariable.Received(1).ApplyLoadedValue(entryToLoad.Value, entryToLoad.Time);
            }
        }

        [Fact]
        public void CanHandleErrorWhileApplying()
        {
            ThrowOnApplyValue = true;
            var entriesToLoad = CreateEntriesToLoad();
            Persistence.EntriesLoaded += Raise.Event<Action<IEnumerable<IPersistenceEntry>>>(entriesToLoad.Values);

            PersistenceProcessor.ApplyLoaded();

            foreach (var persistentVariable in AllPersistentVariables)
            {
                var entryToLoad = entriesToLoad[persistentVariable.Variable.Id];
                persistentVariable.Received().ApplyLoadedValue(entryToLoad.Value, entryToLoad.Time);
            }
        }

        private Dictionary<Guid, IPersistenceEntry> CreateEntriesToLoad()
        {
            var entriesToLoad = new Dictionary<Guid, IPersistenceEntry>();
            var value = 42;
            foreach (var persistentVariable in AllPersistentVariables)
            {
                var entry = Substitute.For<IPersistenceEntry>();
                var guid = persistentVariable.Variable.Id;
                entry.Id.Returns(guid);
                entry.Value.Returns(value);
                entry.Time.Returns(DateTime.UtcNow.AddSeconds(value++));
                entriesToLoad[entry.Id] = entry;
            }
            return entriesToLoad;
        }
    }

    public class PersistChangedPersistentVariables : PersistentVariablesPersistenceProcessorTests
    {
        [Fact]
        public void PersistsChangedVariables()
        {
            var blockWithPersistentVariableToChange = AllBlocks.Skip(2).First();
            var persistentVariableToChange = blockWithPersistentVariableToChange.PersistentVariables.First();
            var unchangedPersistentVariable = blockWithPersistentVariableToChange.PersistentVariables.Skip(1).First();
            blockWithPersistentVariableToChange.PersistentVariables.ElementChanged += Raise.Event<Action<IRuntimePersistentVariable>>(persistentVariableToChange);

            PersistenceProcessor.PersistChanged();

            Persistence.Received().Persist(Arg.Is<IEnumerable<IPersistenceEntry>>(x =>
                x.Any(y => y.Id == persistentVariableToChange.Variable.Id) &&
                x.All(y => y.Id != unchangedPersistentVariable.Variable.Id)));
        }
    }

    public class Dispose
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var processor = CreateEmptyProcessor();

            Action action = processor.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var processor = CreateEmptyProcessor();
            processor.Dispose();

            Action action = processor.Dispose;

            action.Should().NotThrow();
        }

        private static PersistentVariablesPersistenceProcessor CreateEmptyProcessor()
        {
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var persistence = Substitute.For<IPersistence>();
            static IReferenceTypeCloneHandler GetCloneHandler() => new MockCloneHandler();
            var logger = Substitute.For<ILogger>();
            return new PersistentVariablesPersistenceProcessor([], nameCache, persistence, GetCloneHandler, logger);
        }
    }

    private static IRuntimeFunctionBlock CreateBlockWithPersistentVariables(int persistentVariablesCount, Action? applyLoadedValueCallback)
    {
        var block = Substitute.For<IRuntimeFunctionBlock>();
        var persistentVariables = Substitute.For<IRuntimePersistentVariables>();
        var list = new List<IRuntimePersistentVariable>();
        var value = 42;
        for (var i = 0; i < persistentVariablesCount; i++)
        {
            var persistentVariable = Substitute.For<IRuntimePersistentVariable<int>>();
            var guid = Guid.NewGuid();
            persistentVariable.Id.Returns(guid);
            persistentVariable.Variable.Id.Returns(guid);
            persistentVariable.Value.Returns(value++);
            persistentVariable
                .WhenForAnyArgs(x => x.ApplyLoadedValue(default, default))
                .Do((_) => applyLoadedValueCallback?.Invoke());
            list.Add(persistentVariable);
        }
        persistentVariables.GetEnumerator().Returns((_) => list.GetEnumerator());
        block.PersistentVariables.Returns(persistentVariables);
        block.FunctionBlock.PersistentVariables.Returns(persistentVariables.Select(x => x.Variable));
        return block;
    }

    private sealed class MockCloneHandler : IReferenceTypeCloneHandler
    {
        public T? CloneIfNecessary<T>(T? value) => value;
    }
}
