using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultValueAttribute<T>() : DefaultValueAttribute(new T()) where T : new() { }
