using AwesomeAssertions;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class DefaultAggregatingPoolingAttribute_T_Tests
{
    public class Constructor
    {
        [Fact]
        public void InitializesPooling()
        {
            var attribute = new DefaultCrossSourceAggregatingPoolingAttribute<DummyPooling<int>>();

            attribute.Pooling.Should().BeOfType<DummyPooling<int>>();
        }

        private sealed class DummyPooling<TValue> : IAggregatingPooling<TValue>
        {
            public string Name => throw new NotImplementedException();
            public string Description => throw new NotImplementedException();

            public TValue? Aggregate(IEnumerable<TValue?> values) => throw new NotImplementedException();
        }
    }
}
