using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime.Stubs;

internal sealed class EmptyRuntimeDriver : IRuntimeDriver
{
    public void Dispose()
        => Throw();

    public void InitializeI()
        => Throw();

    public void InitializeII()
        => Throw();

    public void Setup(ILogger logger)
        => Throw();

    private static void Throw()
        => throw new InvalidOperationException("This runtime driver is only for design purposes and not intended to be used at runtime.");
}
