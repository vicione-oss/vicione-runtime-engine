using System;

namespace ViciOne.Engine;

public record EngineQueueOptions
{
    internal const int DefaultCommunicationReceiveBufferCapacity = 1_000_000;
    internal const int DefaultPerLinkQueueCapacity = 1_000;

    public int CommunicationReceiveBufferCapacity
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
        }
    } = DefaultCommunicationReceiveBufferCapacity;

    public int PerLinkQueueCapacity
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
        }
    } = DefaultPerLinkQueueCapacity;
}
