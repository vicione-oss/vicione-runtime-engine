namespace ViciOne.Core.Contracts.Runtime;

public enum EngineState
{
    Unstarted,
    Initializing,
    InitializationError,
    Initialized,
    Starting,
    Running,
    Stopping,
    Stopped,
}
