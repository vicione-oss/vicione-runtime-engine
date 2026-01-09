using System.Globalization;

namespace Demo;

public class Derived : Base
{
    public string StringValue => NumericValue.ToString(CultureInfo.InvariantCulture);
}
