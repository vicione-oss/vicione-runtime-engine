using System.ComponentModel;

namespace ViciOne.Core.Contracts.DataModel;

public interface ITypeConversion
{
    TypeConverter TypeConverter { get; }
    bool UseConvertFrom { get; }
}
