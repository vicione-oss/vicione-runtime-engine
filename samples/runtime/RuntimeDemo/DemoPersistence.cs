using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace RuntimeDemo;

internal sealed class DemoPersistence(string path, ILogger logger) : IPersistence
{
    private Dictionary<Guid, IPersistenceEntry> _loadedSettings = [];
    private readonly JsonSerializer _serializer = InitializeSerializer();

    public event Func<IEnumerable<IPersistenceEntry>>? AllValuesToPersistRequested;
    public event Action<IEnumerable<IPersistenceEntry>>? EntriesLoaded;

    private static IEnumerable<IPersistenceEntry> CreateEntriesToPersist(IEnumerable<IPersistenceEntry> entries)
        => entries.Select(x =>
            new DemoPersistenceEntry()
            {
                Time = x.Time,
                Id = x.Id,
                Value = x.Value
            });

    public IEnumerable<IPersistenceEntry> GetPersisted()
    {
        if (File.Exists(path) == false)
            return [];

        try
        {
            IEnumerable<IPersistenceEntry> loadedSettings;
            using (var sr = new StreamReader(path))
            using (var reader = new JsonTextReader(sr))
                loadedSettings = _serializer.Deserialize<DemoPersistenceEntry[]>(reader)!;
            _loadedSettings = loadedSettings
                .ToDictionary(
                    x => x.Id,
                    x => x);
            return loadedSettings;
        }
        catch (Exception ex)
        {
            logger.ReadSettingsFileErrorOccured(ex);
            return [];
        }
    }

    private static JsonSerializer InitializeSerializer()
    {
        var serializer = JsonSerializer.CreateDefault();
        serializer.Formatting = Formatting.Indented;
        return serializer;
    }

    public void LoadPersisted()
        => EntriesLoaded?.Invoke(GetPersisted());

    public void Persist(IEnumerable<IPersistenceEntry> entries)
    {
        IEnumerable<IPersistenceEntry> newerOrNotExisting = entries
            .Where(x =>
                _loadedSettings.TryGetValue(x.Id, out IPersistenceEntry? entry) == false ||
                entry.Time < x.Time);

        bool persist = false;
        foreach (IPersistenceEntry entry in newerOrNotExisting)
        {
            persist = true;
            _loadedSettings[entry.Id] = entry;
        }

        if (persist == false)
            return;

        // TODO: move to a separate thread
        try
        {
            using var sw = new StreamWriter(path);
            using var writer = new JsonTextWriter(sw);
            _serializer.Serialize(writer, CreateEntriesToPersist(_loadedSettings.Values).ToArray());
        }
        catch (Exception ex)
        {
            logger.WriteSettingsFileErrorOccured(ex);
        }
    }

    public IEnumerable<IPersistenceEntry> RequestAllValues()
        => AllValuesToPersistRequested?.Invoke() ?? [];
}
