using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public abstract class DefaultPoolingStrategyAttribute(PoolingStrategy strategy) : Attribute
{
    public virtual PoolingStrategy Strategy { get; } = strategy;
    public abstract PoolingAttributeTarget Target { get; }
}
