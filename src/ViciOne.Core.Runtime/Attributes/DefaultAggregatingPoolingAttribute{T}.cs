using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public abstract class DefaultAggregatingPoolingAttribute<T> : DefaultAggregatingPoolingAttribute where T : IAggregatingPooling, new()
{
    protected DefaultAggregatingPoolingAttribute() : base()
        => Pooling = new T();
}
