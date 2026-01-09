using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Contracts.Runtime.Extensions;

namespace ViciOne.Engine;

internal sealed class RuntimeFunctionBlockProcessor : IDisposable
{
    private readonly HashSet<IRuntimeConnector> _changedConnectors = [];
    private readonly Dictionary<IRuntimeFunctionBlock, FunctionBlockCycleInfo> _cycleInfos;
    private bool _disposed;
    private readonly LinkCache _linkCache;
    private readonly ILogger _logger;
    private readonly IEnumerable<IRuntimeFunctionBlock> _runtimeFunctionBlocks;
    private readonly IUniqueNameProvider _uniqueNameProvider;

    public ulong CycleNo { get; private set; }

    public RuntimeFunctionBlockProcessor(IEnumerable<IRuntimeFunctionBlock> runtimeFunctionBlocks, LinkCache linkCache, IUniqueNameProvider uniqueNameProvider, ILogger logger)
    {
        _runtimeFunctionBlocks = runtimeFunctionBlocks;
        _linkCache = linkCache;
        _uniqueNameProvider = uniqueNameProvider;
        _logger = logger;
        _cycleInfos = InitializeCycleInfos();
    }

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _changedConnectors.Clear();
            _cycleInfos.Clear();
        }

        _disposed = true;
    }

    public IEnumerable<IRuntimeConnector> GetAndResetChangedConnectors()
    {
        var changedConnectorsCopy = _changedConnectors.ToList();
        _changedConnectors.Clear();
        return changedConnectorsCopy;
    }

    private bool GetBlockMustBeProcessed(IRuntimeFunctionBlock block, FunctionBlockCycleInfo cycleInfo)
    {
        switch (block.RunMode)
        {
            case RunMode.Change: return block.GetAndResetChanged();
            case RunMode.Cyclic: return cycleInfo.IsFirstCycle || CycleNo - cycleInfo.LastProcessedCycleNo >= block.CycleFrequency;
            default:
                _logger.FunctionBlockRunModeNotImplemented(block.FunctionBlock.Id, _uniqueNameProvider.GetUniqueName(block.FunctionBlock),
                    block.FunctionBlock.Design.Namespace, block.FunctionBlock.Design.Name, block.RunMode);
                return false;
        }
    }

    private bool GetInputsMustBeProcessed(IRuntimeFunctionBlock block, FunctionBlockCycleInfo cycleInfo, IConnectorValueTransferInterceptor interceptor)
        => cycleInfo.IsFirstCycle || _linkCache.GetIncomingInputLinkGroups(block).Any(x =>
            x.Links.Any(link => link.Changed) ||
            x.NeedsFurtherProcessing ||
            interceptor.IsInterceptionPending(x.Connector.Connector.Id));

    private static bool GetOutputsMustBeProcessed(IRuntimeFunctionBlock block, IConnectorValueTransferInterceptor interceptor)
        => block.GetOutputs().Any(
            x => x.Changed ||
            interceptor.IsInterceptionPending(x.Connector.Id));

    private static bool GetSettingsMustBeProcessed(IRuntimeFunctionBlock block, FunctionBlockCycleInfo cycleInfo)
        => cycleInfo.IsFirstCycle || block.Settings.Any(x => x.Input.Changed || x.Changed);

    private Dictionary<IRuntimeFunctionBlock, FunctionBlockCycleInfo> InitializeCycleInfos()
        => _runtimeFunctionBlocks.ToDictionary(x => x, _ => new FunctionBlockCycleInfo(() => CycleNo));

    private void ProcessBlock(IRuntimeFunctionBlock runtimeBlock, IFunctionBlockCycleInfo cycleInfo)
    {
        try
        {
            runtimeBlock.ProcessCycle(cycleInfo);
        }
        catch (Exception ex)
        {
            _logger.FunctionBlockProcessingErrorOccured(ex, runtimeBlock.FunctionBlock.Id, _uniqueNameProvider.GetUniqueName(runtimeBlock.FunctionBlock),
                runtimeBlock.FunctionBlock.Design.Namespace, runtimeBlock.FunctionBlock.Design.Name);
        }
    }

    public void ProcessBlocks(IPoolingValidator poolingValidator, IReferenceTypeCloneHandler? cloneHandler, IConnectorValueTransferInterceptor interceptor)
    {
        FunctionBlockCycleInfo cycleInfo;
        foreach (IRuntimeFunctionBlock runtimeBlock in _runtimeFunctionBlocks)
        {
            cycleInfo = _cycleInfos[runtimeBlock];
            if (GetInputsMustBeProcessed(runtimeBlock, cycleInfo, interceptor))
                ProcessInputs(runtimeBlock, cycleInfo, poolingValidator, cloneHandler, interceptor);

            // TODO: falls ein Block selbst eine Einstellung ändert, wird ProcessSettings nicht aufgerufen und damit auch das SettingsChanged-Event nicht geworfen
            if (GetSettingsMustBeProcessed(runtimeBlock, cycleInfo))
                ProcessSettings(runtimeBlock, cycleInfo);

            if (GetBlockMustBeProcessed(runtimeBlock, cycleInfo))
            {
                ProcessBlock(runtimeBlock, cycleInfo);
                ResetInputChangedFlags(runtimeBlock);
                ResetSettingsChangedFlags(runtimeBlock);
                cycleInfo.IsFirstCycle = false;
                cycleInfo.LastProcessedCycleNo = CycleNo;
            }

            if (GetOutputsMustBeProcessed(runtimeBlock, interceptor))
                ProcessOutputs(runtimeBlock, cloneHandler, interceptor);
        }

        CycleNo++;
    }

    private void ProcessInputs(IRuntimeFunctionBlock runtimeBlock, FunctionBlockCycleInfo cycleInfo, IPoolingValidator poolingValidator, IReferenceTypeCloneHandler? cloneHandler, IConnectorValueTransferInterceptor interceptor)
    {
        bool anyInputChanged = false;
        bool initialChanged;
        foreach (IIncomingInputLinkGroup group in _linkCache.GetIncomingInputLinkGroups(runtimeBlock))
        {
            initialChanged = group.Connector.Changed;
            try
            {
                if (group.Links.Any(link => link.Changed) ||
                    group.NeedsFurtherProcessing ||
                    interceptor.IsInterceptionPending(group.Connector.Connector.Id))
                    group.Pool(poolingValidator, interceptor);
            }
            catch (Exception ex)
            {
                _logger.FunctionBlockInputConnectorPoolingErrorOccured(ex,
                    group.Connector.Connector.Id, _uniqueNameProvider.GetUniqueName(group.Connector.Connector), runtimeBlock.FunctionBlock.Id,
                    _uniqueNameProvider.GetUniqueName(runtimeBlock.FunctionBlock), runtimeBlock.FunctionBlock.Design.Namespace, runtimeBlock.FunctionBlock.Design.Name);
            }

            if (cycleInfo.IsFirstCycle)
                // im ersten Lauf sollen alle Inputs Changed==true sein
                // TODO: evtl. vom Standardwert abhängig machen?
                group.Connector.MarkAsChanged();

            anyInputChanged |= group.Connector.Changed;

            if (initialChanged != group.Connector.Changed)
                _changedConnectors.Add(group.Connector);
        }

        if (anyInputChanged)
        {
            foreach (IOutgoingInputLinkGroup group in _linkCache.GetOutgoingInputLinkGroups(runtimeBlock))
                try
                {
                    if (group.Connector.Changed)
                        group.TransferValueToLinks(cloneHandler);
                }
                catch (Exception ex)
                {
                    _logger.FunctionBlockInputConnectorValueTransferErrorOccured(ex,
                        group.Connector.Connector.Id, _uniqueNameProvider.GetUniqueName(group.Connector.Connector), runtimeBlock.FunctionBlock.Id,
                        _uniqueNameProvider.GetUniqueName(runtimeBlock.FunctionBlock), runtimeBlock.FunctionBlock.Design.Namespace, runtimeBlock.FunctionBlock.Design.Name);
                }

            runtimeBlock.MarkAsChanged();
        }
    }

    private void ProcessOutputs(IRuntimeFunctionBlock runtimeBlock, IReferenceTypeCloneHandler? cloneHandler, IConnectorValueTransferInterceptor interceptor)
    {
        foreach (IOutputLinkGroup group in _linkCache.GetOutputLinkGroups(runtimeBlock))
            try
            {
                if (group.Connector.GetAndResetChanged() || interceptor.IsInterceptionPending(group.Connector.Connector.Id))
                {
                    group.TransferValueToLinks(cloneHandler, interceptor);
                    _changedConnectors.Add(group.Connector);
                }
            }
            catch (Exception ex)
            {
                _logger.FunctionBlockOutputConnectorValueTransferErrorOccured(ex,
                    group.Connector.Connector.Id, _uniqueNameProvider.GetUniqueName(group.Connector.Connector), runtimeBlock.FunctionBlock.Id,
                    _uniqueNameProvider.GetUniqueName(runtimeBlock.FunctionBlock), runtimeBlock.FunctionBlock.Design.Namespace, runtimeBlock.FunctionBlock.Design.Name);
            }
    }

    private void ProcessSettings(IRuntimeFunctionBlock runtimeBlock, FunctionBlockCycleInfo cycleInfo)
    {
        try
        {
            if (cycleInfo.IsFirstCycle)
                // im ersten Lauf sollen alle Einstellungen Changed==true sein
                // TODO: evtl. vom Standardwert abhängig machen?
                foreach (IRuntimeSetting setting in runtimeBlock.Settings)
                    setting.MarkAsChanged();

            runtimeBlock.Settings.Process(cycleInfo);
        }
        catch (Exception ex)
        {
            _logger.FunctionBlockSettingsProcessingErrorOccured(ex, runtimeBlock.FunctionBlock.Id, _uniqueNameProvider.GetUniqueName(runtimeBlock.FunctionBlock),
                runtimeBlock.FunctionBlock.Design.Namespace, runtimeBlock.FunctionBlock.Design.Name);
        }
    }

    private void ResetInputChangedFlags(IRuntimeFunctionBlock runtimeBlock)
    {
        foreach (IIncomingInputLinkGroup group in _linkCache.GetIncomingInputLinkGroups(runtimeBlock))
            group.Connector.GetAndResetChanged();
    }

    private static void ResetSettingsChangedFlags(IRuntimeFunctionBlock runtimeBlock)
        => runtimeBlock.Settings.ResetChangedFlags();
}
