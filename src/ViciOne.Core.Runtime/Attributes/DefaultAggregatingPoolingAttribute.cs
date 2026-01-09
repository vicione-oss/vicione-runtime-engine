using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime.Stubs;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public abstract class DefaultAggregatingPoolingAttribute : Attribute
{
    public virtual IAggregatingPooling Pooling { get; protected set; } = new EmptyAggregatingPooling<object>();
    public abstract PoolingAttributeTarget Target { get; }
}
