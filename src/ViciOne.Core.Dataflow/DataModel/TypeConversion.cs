using System.ComponentModel;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public class TypeConversion : ITypeConversion
{
    public TypeConverter TypeConverter { get; set; } = new();
    public bool UseConvertFrom { get; set; }
}
