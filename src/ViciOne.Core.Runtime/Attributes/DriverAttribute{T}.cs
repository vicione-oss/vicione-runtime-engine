using System;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DriverAttribute<T>() : DriverAttribute(typeof(T)) where T : IRuntimeDriver { }
