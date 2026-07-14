using System.Collections.Generic;

namespace ViciOne.Engine.Extensions;

internal static class QueueExtensions
{
    extension<T>(Queue<T> queue)
    {
        internal T Dequeue(out int queueCount)
        {
            T value = queue.Dequeue();
            queueCount = queue.Count;
            return value;
        }

        internal void Enqueue(T value, out int queueCount)
        {
            queue.Enqueue(value);
            queueCount = queue.Count;
        }

        internal T[] GetAllAndClear()
        {
            int count = queue.Count;
            if (count == 0)
                return [];
            if (count == 1)
                return [queue.Dequeue()];
            var values = new T[count];
            queue.CopyTo(values, 0);
            queue.Clear();
            return values;
        }
    }
}
