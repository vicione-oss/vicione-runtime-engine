using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace RuntimeDemo;

internal sealed class DemoCommunicationManager : ICommunicationManager, IDisposable
{
    private readonly string _basePath;
    private readonly FileSystemWatcher _directoryWatcher;
    private bool _disposed;
    private readonly HashSet<Guid> _externalIncomingLinkIds;
    private readonly Dictionary<Guid, ConnectorInfo> _externalOutgoingLinkIdsConnectorInfos;
    private readonly IEnumerable<ILink> _links;
    private readonly ILogger _logger;
    private readonly bool _passOutgoingToLogger;
    private readonly JsonSerializer _serializer;

    public event EventHandler<IEnumerable<ILinkChangedEntry>>? ChangedEntriesReceived;

    public DemoCommunicationManager(string basePath, IEnumerable<IFunctionBlock> functionBlocks, IEnumerable<ILink> links, ILogger logger, bool passOutgoingToLogger)
    {
        _basePath = basePath;
        _links = links;
        _logger = logger;
        _passOutgoingToLogger = passOutgoingToLogger;
        _directoryWatcher = InitializeDirectory();
        _serializer = InitializeSerializer();
        _externalIncomingLinkIds = GetExternalLinkIds<IExternalIncomingLink>();
        _externalOutgoingLinkIdsConnectorInfos = InitializeExternalOutgoingLinkIdsWithConnectorInfos(functionBlocks, links);
    }

    private void CreateDirectoryIfNecessary()
        => Directory.CreateDirectory(_basePath);

    private FileSystemWatcher CreateDirectoryWatcher()
    {
        FileSystemWatcher directoryWatcher = new(_basePath, "*.json")
        {
            NotifyFilter =
                NotifyFilters.LastWrite |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName
        };
        directoryWatcher.Changed += FileChanged;
        directoryWatcher.Created += FileChanged;
        directoryWatcher.Renamed += FileChanged;
        directoryWatcher.EnableRaisingEvents = true;
        return directoryWatcher;
    }

    private static IEnumerable<ILinkChangedEntry> CreateEntriesToSave(IEnumerable<ILinkChangedEntry> entries)
        => entries.Select(x =>
            new DemoLinkChangedEntry()
            {
                Time = x.Time,
                Id = x.Id,
                Validity = x.Validity,
                Value = x.Value,
            });

    public void Dispose()
      => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _directoryWatcher.Changed -= FileChanged;
            _directoryWatcher.Created -= FileChanged;
            _directoryWatcher.Renamed -= FileChanged;
            _directoryWatcher.Dispose();
            _externalIncomingLinkIds.Clear();
            ChangedEntriesReceived = null;
        }

        _disposed = true;
    }

    private void FileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (e.Name is not null && IsRelevantExternalIncomingLinkChange(e.Name))
            {
                ILinkChangedEntry loaded = LoadLinkChangedEntry(e.FullPath);
                if (loaded != null)
                    ChangedEntriesReceived?.Invoke(this, Enumerable.Repeat(loaded, 1));
            }
        }
        catch (Exception ex)
        {
            _logger.ReadCommunicationFileErrorOccured(ex, e.Name, ex.Message);
        }
    }

    private static IEnumerable<(Guid, ConnectorInfo)> GetConnectorInfos(IEnumerable<IFunctionBlock> functionBlocks,
        Func<IFunctionBlock, IEnumerable<IConnector>> enumerationSelector, bool isInput, bool isOutput, bool isSetting)
        => functionBlocks
           .Select(block => enumerationSelector(block)
               .Select(connector => (connector.Id, new ConnectorInfo(block, isInput, isOutput, isSetting, connector.Design.Name))))
           .SelectMany(x => x);

    private static string GetConnectorType(ConnectorInfo connectorInfo)
    {
        if (connectorInfo.IsSetting)
        {
            if (connectorInfo.IsInput) return "Einstellungs-Eingang";
            if (connectorInfo.IsOutput) return "Einstellungs-Ausgang";
        }
        else
        {
            if (connectorInfo.IsInput) return "Eingang";
            if (connectorInfo.IsOutput) return "Ausgang";
        }
        return "Unbekannter Typ";
    }

    private HashSet<Guid> GetExternalLinkIds<TLink>() where TLink : IExternalLink
        => [.. _links
            .OfType<TLink>()
            .Select(x => x.Id)];

    private string GetExternalOutgoingPath(ILinkChangedEntry entry)
        => Path.Combine(_basePath, $"external.{entry.Id}.json");

    private static IEnumerable<(Guid, ConnectorInfo)> GetInputInfos(IEnumerable<IFunctionBlock> functionBlocks)
        => GetConnectorInfos(functionBlocks, x => x.ProcessDataInputs, true, false, false);

    private static IEnumerable<(Guid, ConnectorInfo)> GetOutputInfos(IEnumerable<IFunctionBlock> functionBlocks)
        => GetConnectorInfos(functionBlocks, x => x.ProcessDataOutputs, false, true, false);

    private static IEnumerable<(Guid, ConnectorInfo)> GetSettingsInputInfos(IEnumerable<IFunctionBlock> functionBlocks)
        => GetConnectorInfos(functionBlocks, x => x.SettingsInputs, true, false, true);

    private static IEnumerable<(Guid, ConnectorInfo)> GetSettingsOutputInfos(IEnumerable<IFunctionBlock> functionBlocks)
        => GetConnectorInfos(functionBlocks, x => x.SettingsOutputs, false, true, true);

    public void HandleChangedOutgoingLinks(IEnumerable<ILinkChangedEntry> externalLinkEntries)
        // TODO: possibly move to a separate thread
        => HandleChangedOutgoingLinks(externalLinkEntries, GetExternalOutgoingPath,
            (entry, ex) => _logger.OutgoingLinkHandlingErrorOccured(ex, entry.Id));

    private void HandleChangedOutgoingLinks(IEnumerable<ILinkChangedEntry> entries, Func<ILinkChangedEntry, string> getPath, Action<ILinkChangedEntry, Exception> onError)
    {
        string path;
        foreach (ILinkChangedEntry entry in CreateEntriesToSave(entries))
        {
            if (_passOutgoingToLogger)
                LogChangedOutgoingLink(entry, _logger);

            path = getPath(entry);

            try
            {
                using var sw = new StreamWriter(path);
                using var writer = new JsonTextWriter(sw);
                _serializer.Serialize(writer, entry);
            }
            catch (Exception ex)
            {
                onError(entry, ex);
            }
        }
    }

    private static Dictionary<Guid, ConnectorInfo> InitializeExternalOutgoingLinkIdsWithConnectorInfos(IEnumerable<IFunctionBlock> functionBlocks, IEnumerable<ILink> links)
    {
        IEnumerable<IExternalOutgoingLink> externalOutgoingLinks = links.OfType<IExternalOutgoingLink>();

        IEnumerable<(Guid, ConnectorInfo)> inputs = GetInputInfos(functionBlocks);
        IEnumerable<(Guid, ConnectorInfo)> outputs = GetOutputInfos(functionBlocks);
        IEnumerable<(Guid, ConnectorInfo)> settingsInputs = GetSettingsInputInfos(functionBlocks);
        IEnumerable<(Guid, ConnectorInfo)> settingsOutputs = GetSettingsOutputInfos(functionBlocks);

        var connectors = inputs
            .Concat(outputs)
            .Concat(settingsInputs)
            .Concat(settingsOutputs)
            .ToDictionary(x => x.Item1, x => x.Item2);

        return externalOutgoingLinks
            .ToDictionary(x => x.Id, x => connectors[x.Source.Id]);
    }

    private FileSystemWatcher InitializeDirectory()
    {
        CreateDirectoryIfNecessary();
        FileSystemWatcher directoryWatcher = CreateDirectoryWatcher();
        return directoryWatcher;
    }

    private static JsonSerializer InitializeSerializer()
        => JsonSerializer.CreateDefault();

    private bool IsRelevantExternalIncomingLinkChange(string name)
    {
        string[] parts = Path.GetFileNameWithoutExtension(name).Split('.');
        if (parts.Length != 3 || string.Equals(parts[0], "external", StringComparison.OrdinalIgnoreCase) == false)
            return false;
        var linkId = Guid.Parse(parts[2]);
        return _externalIncomingLinkIds.Contains(linkId);
    }

    private DemoLinkChangedEntry LoadLinkChangedEntry(string path)
    {
        using var sr = new StreamReader(path);
        using var reader = new JsonTextReader(sr);
        return _serializer.Deserialize<DemoLinkChangedEntry>(reader)!;
    }

    private void LogChangedOutgoingLink(ILinkChangedEntry entry, ILogger logger)
    {
        if (_externalOutgoingLinkIdsConnectorInfos.TryGetValue(entry.Id, out ConnectorInfo? connectorInfo) == false)
        {
            logger.UnknownOutgoingLinkChanged(entry.Time, entry.Id, entry.Validity, entry.Value);
            return;
        }

        string blockName = connectorInfo.Block.Name;
        string connectorName = connectorInfo.Name;
        string fullName = $"{blockName}.{connectorName}";
        string type = GetConnectorType(connectorInfo);
        logger.KnownOutgoingLinkChanged(fullName, type, entry.Time, entry.Validity, entry.Value);
    }

    private sealed class ConnectorInfo
    {
        public IFunctionBlock Block { get; set; }
        public bool IsInput { get; set; }
        public bool IsOutput { get; set; }
        public bool IsSetting { get; set; }
        public string Name { get; set; }

        public ConnectorInfo(IFunctionBlock block, bool isInput, bool isOutput, bool isSetting, string name)
            => (Block, IsInput, IsOutput, IsSetting, Name) = (block, isInput, isOutput, isSetting, name);
    }
}
