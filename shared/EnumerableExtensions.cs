using System.Collections.Generic;
using System.Linq;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
internal static class EnumerableExtensions
{
    extension<TSource>(IEnumerable<TSource> first)
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1851:Possible multiple enumerations",
            Justification = "Multiple enumerations are accepted here to avoid additional memory usage.")]
        public bool SequenceEqualIgnoreOrder(IEnumerable<TSource> second)
            => first.Count() == second.Count() &&
               first.Except(second).Any() == false;
    }
}
