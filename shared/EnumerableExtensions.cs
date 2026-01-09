using System.Collections.Generic;
using System.Linq;

internal static class EnumerableExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1851:Mögliche mehrere Enumerationen der „IEnumerable“-Auflistung",
        Justification = "Mögliche mehrere Enumerationen werden hier in Kauf genommen um zusätzlichen Speicherverbauch zu vermeiden.")]
    public static bool SequenceEqualIgnoreOrder<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        => first.Count() == second.Count() &&
           first.Except(second).Any() == false;
}
