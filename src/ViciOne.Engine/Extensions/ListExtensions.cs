using System.Collections.Generic;

namespace ViciOne.Engine.Extensions;

internal static class ListExtensions
{
    extension<T>(List<T> list)
    {
        internal void AddRangeWithDropNewest(IEnumerable<T> items, int capacity, ref long dropped)
        {
            // Fallback for non-ICollection enumerables
            if (items is not ICollection<T> collection)
            {
                foreach (T item in items)
                    if (list.Count < capacity)
                        list.Add(item);
                    else
                        dropped++;

                return;
            }

            // Fast path for collections already providing a count property
            int count = collection.Count;
            int available = capacity - list.Count;
            if (count <= available)
                // Common path - entire batch fits: bulk add of complete batch
                list.AddRange(collection);
            else if (available > 0)
            {
                // Partial overflow: add what fits, drop the rest
                foreach (T item in collection)
                {
                    if (list.Count >= capacity)
                        break;
                    list.Add(item);
                }
                dropped += count - available;
            }
            else
                // Already at capacity: drop entire batch
                dropped += count;
        }
    }
}
