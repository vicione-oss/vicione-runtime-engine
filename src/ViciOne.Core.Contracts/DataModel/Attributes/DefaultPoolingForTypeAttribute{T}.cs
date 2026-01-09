using System;

namespace ViciOne.Core.Contracts.DataModel.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class DefaultPoolingForTypeAttribute<T>() : DefaultPoolingForTypeAttribute(typeof(T)) { }
