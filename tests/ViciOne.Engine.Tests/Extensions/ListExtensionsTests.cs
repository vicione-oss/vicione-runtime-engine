using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using ViciOne.Engine.Extensions;
using Xunit;

public class ListExtensionsTests
{
    public class AddRangeWithDropNewest
    {
        public static TheoryData<IEnumerable<object>, int> ArgumentsFor_AddsItemsThatFit
        {
            get
            {
                object[] itemsAsArray = [11, 12, 13];
                var itemsAsEnumerable = itemsAsArray.Select(x => x);

                return new()
                {
                    { itemsAsArray, 10 },
                    { itemsAsArray, 6 },
                    { itemsAsArray, 4 },
                    { itemsAsArray, 3 },
                    { itemsAsEnumerable, 10 },
                    { itemsAsEnumerable, 6 },
                    { itemsAsEnumerable, 4 },
                    { itemsAsEnumerable, 3 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_AddsItemsThatFit))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1851:Possible multiple enumerations of 'IEnumerable' collection", Justification = "Not relevant for these tests.")]
        public void AddsItemsThatFit(IEnumerable<object> itemsToAdd, int capacity)
        {
            List<object> list = [1, 2, 3];
            var expectedItemsToAddCount = Math.Min(itemsToAdd.Count(), capacity - list.Count);
            var expectedList = list.Concat(itemsToAdd.Take(expectedItemsToAddCount)).ToArray();
            long dropped = 0;

            list.AddRangeWithDropNewest(itemsToAdd, capacity, ref dropped);

            list.Should().BeEquivalentTo(expectedList, o => o.WithStrictOrdering());
        }

        public static TheoryData<IEnumerable<object>, int, int> ArgumentsFor_IncreasesNumberOfDroppedItems
        {
            get
            {
                object[] itemsAsArray = [11, 12, 13];
                var itemsAsEnumerable = itemsAsArray.Select(x => x);

                return new()
                {
                    { itemsAsArray, 10, 0 },
                    { itemsAsArray, 6, 0 },
                    { itemsAsArray, 4, 2 },
                    { itemsAsArray, 3, 3 },
                    { itemsAsEnumerable, 10, 0 },
                    { itemsAsEnumerable, 6, 0 },
                    { itemsAsEnumerable, 4, 2 },
                    { itemsAsEnumerable, 3, 3 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_IncreasesNumberOfDroppedItems))]
        public void IncreasesNumberOfDroppedItems(IEnumerable<object> itemsToAdd, int capacity, int expectedDropped)
        {
            const int InitialDropped = 5;

            List<object> list = [1, 2, 3];
            long dropped = InitialDropped;

            list.AddRangeWithDropNewest(itemsToAdd, capacity, ref dropped);

            dropped.Should().Be(InitialDropped + expectedDropped);
        }
    }
}
