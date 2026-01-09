using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultCrossSourcePoolingStrategyAttribute(PoolingStrategy strategy) : DefaultPoolingStrategyAttribute(strategy)
{
    public override PoolingAttributeTarget Target => PoolingAttributeTarget.CrossSource;
}
