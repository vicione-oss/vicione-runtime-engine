using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Filters;

public static class DriverFilter
{
    public static IEnumerable<string> GetDistinctRuntimeDriverTypeNames(IEnumerable<IFunctionBlock> blocks)
        => blocks
            .Where(x => x.Design.RuntimeDriverTypeName is not null)
            .Select(x => x.Design.RuntimeDriverTypeName!)
            .Distinct(StringComparer.Ordinal);
}
