using AwesomeAssertions;
using System.Collections.Generic;
using ViciOne.Engine.Extensions;
using Xunit;

public class QueueExtensionsTests
{
    public class Dequeue
    {
        [Fact]
        public void DequeuesEntry()
        {
            List<object> list = [1, 2, 3];
            Queue<object> queue = new(list);
            var oldestValue = queue.Peek();

            var result = queue.Dequeue(out _);

            result.Should().Be(oldestValue);
        }

        [Fact]
        public void ReturnsCorrectQueueCount()
        {
            List<object> list = [1, 2, 3];
            Queue<object> queue = new(list);

            queue.Dequeue(out var queueCount);

            queueCount.Should().Be(queue.Count);
        }
    }

    public class Enqueue
    {
        [Fact]
        public void EnqueuesEntry()
        {
            List<object> list = [1, 2, 3];
            Queue<object> queue = new(list);
            var newValue = 4;
            List<object> expectedListAfterEnqueue = [.. list, newValue];

            queue.Enqueue(newValue, out _);

            queue.Should().BeEquivalentTo(expectedListAfterEnqueue, o => o.WithStrictOrdering());
        }

        [Fact]
        public void ReturnsCorrectQueueCount()
        {
            List<object> list = [1, 2, 3];
            Queue<object> queue = new(list);
            var newValue = 4;

            queue.Enqueue(newValue, out var queueCount);

            queueCount.Should().Be(queue.Count);
        }
    }

    public class GetAllAndClear
    {
        [Fact]
        public void GetsAllEntries()
        {
            List<object> list = [1, 2, 3];
            Queue<object> queue = new(list);

            var result = queue.GetAllAndClear();

            result.Should().BeEquivalentTo(list, o => o.WithStrictOrdering());
        }

        [Fact]
        public void ClearsAllEntries()
        {
            List<object> list = [1, 2, 3];
            Queue<object> queue = new(list);

            queue.GetAllAndClear();

            queue.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsArrayEmptySingletonIfQueueIsEmpty()
        {
            Queue<object> queue = new();

            var result = queue.GetAllAndClear();

            result.Should().BeSameAs([]);
        }
    }
}
