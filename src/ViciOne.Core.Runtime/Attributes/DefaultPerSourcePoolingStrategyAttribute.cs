using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultPerSourcePoolingStrategyAttribute(PoolingStrategy strategy) : DefaultPoolingStrategyAttribute(strategy)
{
    public override PoolingAttributeTarget Target => PoolingAttributeTarget.PerSource;
}
