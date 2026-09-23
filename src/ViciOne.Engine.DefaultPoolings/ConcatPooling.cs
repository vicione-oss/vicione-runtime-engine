using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultPoolings;

public class ConcatPooling : IAggregatingPooling<string>
{
    public string Description => "Concatenates all current input instances of string.";
    public string Name => "Concat";

    // string.Join reads a list or an array as a span and determines the total length first, so it allocates
    // the result and nothing else. string.Concat has no such fast path and boxes the enumerator instead.
    public string? Aggregate(IEnumerable<string?> values)
        => string.Join(string.Empty, values);
}
