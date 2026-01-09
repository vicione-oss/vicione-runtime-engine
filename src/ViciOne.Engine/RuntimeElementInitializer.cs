using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class RuntimeElementInitializer<TRuntimeElement> where TRuntimeElement : notnull
{
    protected (IEnumerable<TRuntimeElement> Instances, IDictionary<TRuntimeElement, Exception> FailedBlocks)
        InitializeInstance(IEnumerable<TRuntimeElement> elements, Action<TRuntimeElement> initCall, byte phaseNo, IUniqueNameProvider uniqueNameProvider, ILogger logger)
    {
        var failedElements = new Dictionary<TRuntimeElement, Exception>();
        foreach (TRuntimeElement element in elements)
        {
            try { initCall(element); }
            catch (Exception ex)
            {
                LogInitializationError(ex, element, phaseNo, uniqueNameProvider, logger);
                failedElements.Add(element, ex);
            }
        }
        return (elements.Except(failedElements.Keys), failedElements);
    }

    protected abstract void LogInitializationError(Exception exception, TRuntimeElement element, byte phaseNo, IUniqueNameProvider uniqueNameProvider, ILogger logger);
}
