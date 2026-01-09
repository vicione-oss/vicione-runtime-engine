using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class PersistenceEntryFactoryTests
{
    public class Create
    {
        [Fact]
        public void CanCreateInstance()
        {
            var id = Guid.NewGuid();
            var time = DateTime.Now;
            var connectorValue = 42;
            var expectedValue = 666;
            var runtimeSetting = Substitute.For<IPersistable<int>>();
            runtimeSetting.Id.Returns(id);
            runtimeSetting.ChangedTime.Returns(time);
            runtimeSetting.Value.Returns(connectorValue);
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();
            cloneHandler.CloneIfNecessary<int>(default).ReturnsForAnyArgs(expectedValue);
            using var factory = new PersistenceEntryFactory();

            var entry = factory.Create(runtimeSetting, cloneHandler);

            entry.Should().BeAssignableTo<IPersistenceEntry<int>>().Which.Should()
                .BeEquivalentTo(new
                {
                    Time = time,
                    Id = id,
                    Value = expectedValue,
                });
        }

        [Fact]
        public void ClonesValue()
        {
            var setting = Substitute.For<IPersistable<int>>();
            var cloneHandler = Substitute.For<IReferenceTypeCloneHandler>();
            using var factory = new PersistenceEntryFactory();

            factory.Create(setting, cloneHandler);

            cloneHandler.Received(1).CloneIfNecessary(Arg.Any<int>());
        }
    }
}
