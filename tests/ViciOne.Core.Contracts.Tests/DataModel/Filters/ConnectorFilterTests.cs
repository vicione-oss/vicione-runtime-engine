using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Filters;
using Xunit;

public class ConnectorFilterTests
{
    public class GetConnectorByName
    {
        [Fact]
        public void ReturnsCorrectConnectorIfExisting()
        {
            var connector1 = CreateConnector("Eins");
            var connector2 = CreateConnector("Zwei");
            var connector3 = CreateConnector("Drei");
            var connectors = new List<IConnector>()
            {
                connector1,
                connector2,
                connector3,
            };

            var returned = ConnectorFilter.GetConnectorByName(connectors, "Zwei");

            returned.Should().NotBeNull();
            returned.Should().BeSameAs(connector2);
        }

        [Fact]
        public void ReturnsNullIfNotExisting()
        {
            var connector1 = CreateConnector("Eins");
            var connector2 = CreateConnector("Zwei");
            var connector3 = CreateConnector("Drei");
            var connectors = new List<IConnector>()
            {
                connector1,
                connector2,
                connector3,
            };

            var returned = ConnectorFilter.GetConnectorByName(connectors, "Vier");

            returned.Should().BeNull();
        }

        private static IConnector CreateConnector(string name)
        {
            var connector = Substitute.For<IConnector>();
            connector.Design.Name.Returns(name);
            return connector;
        }
    }

    public class GetConnectorById
    {
        [Fact]
        public void ReturnsCorrectConnectorIfExisting()
        {
            var connector1 = CreateConnector(Guid.Parse("fcc189cb-3b47-4df7-a962-01ec257aece8"));
            var connector2 = CreateConnector(Guid.Parse("653aad51-8e3f-4dbd-a4e4-2a2c78885798"));
            var connector3 = CreateConnector(Guid.Parse("de997594-c57b-47a4-91f3-59a0c335e0b0"));
            var connectors = new List<IConnector>()
            {
                connector1,
                connector2,
                connector3,
            };

            var returned = ConnectorFilter.GetConnectorById(connectors, Guid.Parse("653aad51-8e3f-4dbd-a4e4-2a2c78885798"));

            returned.Should().NotBeNull();
            returned.Should().BeSameAs(connector2);
        }

        [Fact]
        public void ReturnsNullIfNotExisting()
        {
            var connector1 = CreateConnector(Guid.Parse("fcc189cb-3b47-4df7-a962-01ec257aece8"));
            var connector2 = CreateConnector(Guid.Parse("653aad51-8e3f-4dbd-a4e4-2a2c78885798"));
            var connector3 = CreateConnector(Guid.Parse("de997594-c57b-47a4-91f3-59a0c335e0b0"));
            var connectors = new List<IConnector>()
            {
                connector1,
                connector2,
                connector3,
            };

            var returned = ConnectorFilter.GetConnectorById(connectors, Guid.Parse("313657f0-5b02-4be5-9b53-ffbea833d32e"));

            returned.Should().BeNull();
        }

        private static IConnector CreateConnector(Guid id)
        {
            var connector = Substitute.For<IConnector>();
            connector.Id.Returns(id);
            return connector;
        }
    }
}
