using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultPerSourceAggregatingPoolingAttribute<T>() : DefaultAggregatingPoolingAttribute<T>() where T : IAggregatingPooling, new()
{
    public override PoolingAttributeTarget Target => PoolingAttributeTarget.PerSource;
}
