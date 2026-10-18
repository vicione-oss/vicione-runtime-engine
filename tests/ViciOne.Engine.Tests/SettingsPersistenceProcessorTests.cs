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
public abstract class SettingsPersistenceProcessorTests : IDisposable
{
    private protected IEnumerable<IRuntimeFunctionBlock> AllBlocks { get; }
    private protected IEnumerable<IRuntimeSetting> AllSettings { get; }
    private protected virtual Action? ApplyLoadedValueCallback { get; }
    private protected TestLogger Logger { get; } = new();
    private protected MockPersistence Persistence { get; } = Substitute.ForPartsOf<MockPersistence>();
    private protected SettingsPersistenceProcessor PersistenceProcessor { get; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The created FBs are assigned to the AllBlocks property and are disposed through it as well.")]
    protected SettingsPersistenceProcessorTests()
    {
        IRuntimeFunctionBlock[] fbs =
        [
            CreateBlockWithSettings(1, ApplyLoadedValueCallback),
            CreateBlockWithSettings(0, ApplyLoadedValueCallback),
            CreateBlockWithSettings(2, ApplyLoadedValueCallback),
        ];
        IReferenceTypeCloneHandler cloneHandler = new MockCloneHandler();
        IReferenceTypeCloneHandler GetCloneHandler() => cloneHandler;
        var dataflow = Substitute.For<IDataflow>();
        dataflow.RootContainer.FunctionBlocks.Returns(fbs.Select(x => x.FunctionBlock));
        var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);
        var nameCache = new DefaultUniqueNameProvider(uniqueNames);

        PersistenceProcessor = new SettingsPersistenceProcessor(fbs, nameCache, Persistence, GetCloneHandler, Logger);
        AllBlocks = fbs;
        AllSettings = fbs.SelectMany(x => x.Settings);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not relevant for the tests")]
    void IDisposable.Dispose()
    {
        foreach (var block in AllBlocks)
            block.Dispose();
        PersistenceProcessor.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual ILogger CreateLogger() => Substitute.For<ILogger>();
    protected virtual IPersistence CreatePersistence() => Substitute.For<IPersistence>();

    public class Constructor : SettingsPersistenceProcessorTests
    {
        [Fact]
        public void CanHandleUnknownSettingsWhenLoaded()
        {
            var entries = new List<IPersistenceEntry>();
            var notExistingEntry = Substitute.For<IPersistenceEntry<object>>();
            var notExistingGuid = Guid.NewGuid();
            notExistingEntry.Id.Returns(notExistingGuid);
            entries.Add(notExistingEntry);
            var existingEntry = Substitute.For<IPersistenceEntry<int>>();
            var existingGuid = AllSettings.First().Setting.Id;
            existingEntry.Id.Returns(existingGuid);
            entries.Add(existingEntry);

            Persistence.EntriesLoaded += Raise.Event<Action<IEnumerable<IPersistenceEntry>>>(entries);

            var loggedMessages = Logger.GetLoggedMessages();
            loggedMessages.Should()
                .ContainSingle(x => x.Contains(notExistingGuid.ToString(), StringComparison.OrdinalIgnoreCase)).And
                .NotContain(x => x.Contains(existingGuid.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ReturnsAllSettingsValuesToPersistenceWhenRequested()
        {
            var settingsValues = Persistence.RequestAllValues();

            settingsValues.Should().HaveSameCount(AllSettings);
            foreach (var setting in AllSettings)
            {
                settingsValues.Should()
                    .ContainSingle(x => x.Id == setting.Setting.Id)
                        .Which.Time.Should().Be(setting.ChangedTime);
            }
        }
    }

    public class ApplyLoaded : SettingsPersistenceProcessorTests
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

            foreach (var setting in AllSettings)
            {
                var entryToLoad = entriesToLoad[setting.Setting.Id];
                setting.Received().ApplyLoadedValue(entryToLoad.Value, entryToLoad.Time);
            }
        }

        [Fact]
        public void AppliesOnlyOnce()
        {
            var entriesToLoad = CreateEntriesToLoad();
            Persistence.EntriesLoaded += Raise.Event<Action<IEnumerable<IPersistenceEntry>>>(entriesToLoad.Values);

            PersistenceProcessor.ApplyLoaded();
            PersistenceProcessor.ApplyLoaded();

            foreach (var setting in AllSettings)
            {
                var entryToLoad = entriesToLoad[setting.Setting.Id];
                setting.Received(1).ApplyLoadedValue(entryToLoad.Value, entryToLoad.Time);
            }
        }

        [Fact]
        public void CanHandleErrorWhileApplying()
        {
            ThrowOnApplyValue = true;
            var entriesToLoad = CreateEntriesToLoad();
            Persistence.EntriesLoaded += Raise.Event<Action<IEnumerable<IPersistenceEntry>>>(entriesToLoad.Values);

            PersistenceProcessor.ApplyLoaded();

            foreach (var setting in AllSettings)
            {
                var entryToLoad = entriesToLoad[setting.Setting.Id];
                setting.Received().ApplyLoadedValue(entryToLoad.Value, entryToLoad.Time);
            }
        }

        private Dictionary<Guid, IPersistenceEntry> CreateEntriesToLoad()
        {
            var entriesToLoad = new Dictionary<Guid, IPersistenceEntry>();
            var value = 42;
            foreach (var setting in AllSettings)
            {
                var entry = Substitute.For<IPersistenceEntry>();
                var guid = setting.Setting.Id;
                entry.Id.Returns(guid);
                entry.Value.Returns(value);
                entry.Time.Returns(DateTime.UtcNow.AddSeconds(value++));
                entriesToLoad[entry.Id] = entry;
            }
            return entriesToLoad;
        }
    }

    public class PersistChanged : SettingsPersistenceProcessorTests
    {
        [Fact]
        public void PersistsChangedSettings()
        {
            var blockWithSettingToChange = AllBlocks.Skip(2).First();
            var settingToChange = blockWithSettingToChange.Settings.First();
            var unchangedSetting = blockWithSettingToChange.Settings.Skip(1).First();
            blockWithSettingToChange.Settings.ElementChanged += Raise.Event<Action<IRuntimeSetting>>(settingToChange);

            PersistenceProcessor.PersistChanged();

            Persistence.Received().Persist(Arg.Is<IEnumerable<IPersistenceEntry>>(x =>
                x.Any(y => y.Id == settingToChange.Setting.Id) &&
                x.All(y => y.Id != unchangedSetting.Setting.Id)));
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

        private static SettingsPersistenceProcessor CreateEmptyProcessor()
        {
            var nameCache = new DefaultUniqueNameProvider(new Dictionary<Guid, string>());
            var persistence = Substitute.For<IPersistence>();
            static IReferenceTypeCloneHandler GetCloneHandler() => new MockCloneHandler();
            var logger = Substitute.For<ILogger>();
            return new SettingsPersistenceProcessor([], nameCache, persistence, GetCloneHandler, logger);
        }
    }

    private static IRuntimeFunctionBlock CreateBlockWithSettings(int settingsCount, Action? applyLoadedValueCallback)
    {
        var block = Substitute.For<IRuntimeFunctionBlock>();
        var settings = Substitute.For<IRuntimeSettings>();
        var list = new List<IRuntimeSetting>();
        var value = 42;
        for (var i = 0; i < settingsCount; i++)
        {
            var setting = Substitute.For<IRuntimeSetting<int>>();
            var guid = Guid.NewGuid();
            setting.Id.Returns(guid);
            setting.Setting.Id.Returns(guid);
            setting.Value.Returns(value++);
            setting
                .WhenForAnyArgs(x => x.ApplyLoadedValue(default, default))
                .Do((_) => applyLoadedValueCallback?.Invoke());
            list.Add(setting);
        }
        settings.GetEnumerator().Returns((_) => list.GetEnumerator());
        block.Settings.Returns(settings);
        block.FunctionBlock.Settings.Returns(settings.Select(x => x.Setting));
        return block;
    }

    private sealed class MockCloneHandler : IReferenceTypeCloneHandler
    {
        public T? CloneIfNecessary<T>(T? value) => value;
    }
}
