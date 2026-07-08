using AwesomeAssertions;
using System.Collections.Generic;
using ViciOne.Engine.Extensions;
using Xunit;

public class QueueExtensionsTests
{
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
