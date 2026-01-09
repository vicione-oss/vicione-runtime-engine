using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime;

public abstract class RuntimeDriver<TDriver> : IRuntimeDriver where TDriver : RuntimeDriver<TDriver>, new()
{
    private bool _setupCalled;

    protected ILogger Logger { get; private set; } = NullLogger.Instance;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }

    public virtual void InitializeI() { }

    public virtual void InitializeII() { }

    public void Setup(ILogger logger)
    {
        if (_setupCalled)
            throw new InvalidOperationException($"{nameof(Setup)} was already called. It is not allowed to call it again.");
        _setupCalled = true;

        Logger = logger;
    }
}
