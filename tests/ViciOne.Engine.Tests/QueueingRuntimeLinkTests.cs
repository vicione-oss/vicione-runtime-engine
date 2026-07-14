using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.ComponentModel;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Engine;
using Xunit;

public class QueueingRuntimeLinkTests
{
    public class TryDequeueValue
    {
        [Fact]
        public void ReturnsTrueIfValueWasDequeued()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            var value1 = 42;
            var value2 = 666;
            runtimeLink.SetData(value1, default, default);
            runtimeLink.SetData(value2, default, default);

            var result = runtimeLink.TryDequeueValue(out var dequeued);

            result.Should().BeTrue();
            dequeued.Should().Be(value1);
        }

        [Fact]
        public void ReturnsFalseIfQueueWasEmpty()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);

            var result = runtimeLink.TryDequeueValue(out _);

            result.Should().BeFalse();
        }

        [Fact]
        public void RemovesReturnedValueFromQueue()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            var value1 = 42;
            var value2 = 666;
            runtimeLink.SetData(value1, default, default);
            runtimeLink.SetData(value2, default, default);
            object?[] expectedQueueAfterDequeue = [value2];

            runtimeLink.TryDequeueValue(out _);

            var queued = runtimeLink.GetAndClearQueuedValues();
            queued.Should().BeEquivalentTo(expectedQueueAfterDequeue);
        }
    }

    public class GetAndClearQueuedValues
    {
        [Fact]
        public void ReturnsQueuedValues()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            var value1 = 42;
            var value2 = 666;
            runtimeLink.SetData(value1, default, default);
            runtimeLink.SetData(value2, default, default);
            object?[] expected = [value1, value2];

            var queued = runtimeLink.GetAndClearQueuedValues();

            queued.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ClearsQueuedValues()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            runtimeLink.SetData(42, default, default);
            runtimeLink.SetData(666, default, default);

            runtimeLink.GetAndClearQueuedValues();

            var queued = runtimeLink.GetAndClearQueuedValues();
            queued.Should().BeEmpty();
        }
    }

    public class SetData
    {
        [Fact]
        public void EnqueuesValue()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            var valueToSet = 42;

            runtimeLink.SetData(valueToSet, default, default);

            var queued = runtimeLink.GetAndClearQueuedValues();
            queued.Should().Contain(valueToSet);
        }

        [Fact]
        public void EnqueuesConvertedValue()
        {
            var link = Substitute.For<ILink<int>>();
            link.TypeConversion!.UseConvertFrom.Returns(true);
            link.TypeConversion!.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));
            var runtimeLink = new MockQueueingRuntimeLink<string, int>(link, "0", EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            var valueToSet = "42";
            var expectedEnqueuedValue = 42;

            runtimeLink.SetData(valueToSet, default, default);

            var queued = runtimeLink.GetAndClearQueuedValues();
            queued.Should().Contain(expectedEnqueuedValue);
        }

        [Fact]
        public void CallsSetConvertedData()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            var valueToSet = 42;
            var changedTimeToSet = DateTime.UtcNow;
            var validityToSet = Validity.Valid;

            runtimeLink.SetData(valueToSet, changedTimeToSet, validityToSet);

            runtimeLink.SetConvertedDataArgs.Should().BeEquivalentTo((valueToSet, changedTimeToSet, validityToSet));
        }

        [Fact]
        public void DropsOldestEntryIfCapacityReached()
        {
            const int QueueCapacity = 10;

            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, QueueCapacity, NullLogger.Instance);
            var valuesToSet = QueueCapacity + 5;
            var expectedQueued = Enumerable.Range(0 - QueueCapacity + valuesToSet, QueueCapacity);

            for (var valueToSet = 0; valueToSet < valuesToSet; valueToSet++)
                runtimeLink.SetData(valueToSet, default, default);

            var queued = runtimeLink.GetAndClearQueuedValues();
            queued.Should().BeEquivalentTo(expectedQueued);
        }

        [Fact]
        public void LogsWarningOnQueueOverflow()
        {
            const int QueueCapacity = 10;

            var link = CreateLink<object>();
            var logger = new TestLogger();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, QueueCapacity, logger);
            var valuesToSet = QueueCapacity + 5;
            var valueToSet = 42;
            var expectedValuesToDrop = valuesToSet - QueueCapacity;

            for (var i = 0; i < valuesToSet; i++)
                runtimeLink.SetData(valueToSet, default, default);

            var warnings = logger.GetLoggedMessages(LogLevel.Warning).ToArray();
            warnings.Should().HaveCount(expectedValuesToDrop);
            for (var i = 0; i < expectedValuesToDrop; i++)
                warnings[i].Should().MatchEquivalentOf($"*overflow*drop*oldest*capacity*{QueueCapacity}*total*{i + 1}*{nameof(EngineQueueOptions.PerLinkQueueCapacity)}*");
        }
    }

    public class StoreValue
    {
        [Fact]
        public void StoresValue()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockQueueingRuntimeLink<object, object>(link, default, EngineQueueOptions.DefaultPerLinkQueueCapacity, NullLogger.Instance);
            var valueToStore = 42;

            runtimeLink.StoreValue(valueToStore);

            runtimeLink.Value.Should().Be(valueToStore);
        }
    }

    private static ILink<TValue> CreateLink<TValue>()
    {
        var link = Substitute.For<ILink<TValue>>();
        link.TypeConversion.Returns((ITypeConversion?)null);
        return link;
    }

    private sealed class MockQueueingRuntimeLink<TSource, TValue>(ILink<TValue> link, TSource? value, int bufferCapacity, ILogger logger) :
        QueueingRuntimeLink<TSource, TValue>(link, value, bufferCapacity, logger)
    {
        public (TValue? Value, DateTime ChangedTime, Validity Validity)? SetConvertedDataArgs { get; private set; }

        protected override void SetConvertedData(TValue? value, DateTime changedTime, Validity validity)
        {
            SetConvertedDataArgs = (value, changedTime, validity);
            base.SetConvertedData(value, changedTime, validity);
        }
    }
}
