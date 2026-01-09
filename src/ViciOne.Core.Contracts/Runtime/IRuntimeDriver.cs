using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeDriver : IDisposable
{
    void InitializeI();
    void InitializeII();
    void Setup(ILogger logger);
}
