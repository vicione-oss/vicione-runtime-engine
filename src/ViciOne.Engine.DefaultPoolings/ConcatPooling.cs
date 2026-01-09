using System.Collections.Generic;
using System.Text;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class ConcatPooling : IAggregatingPooling<string>
{
    public string Description => "Concatenates all current input instances of string.";
    public string Name => "Concat";

    public string? Aggregate(IEnumerable<string?> values)
    {
        var builder = new StringBuilder();
        foreach (string? value in values)
            builder.Append(value);
        return builder.ToString();
    }
}
