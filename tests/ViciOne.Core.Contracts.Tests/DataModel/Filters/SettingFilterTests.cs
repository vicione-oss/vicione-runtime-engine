using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Filters;
using Xunit;

public class SettingFilterTests
{
    public class GetSettingByName
    {
        [Fact]
        public void ReturnsCorrectSettingIfExisting()
        {
            var setting1 = CreateSetting("Eins");
            var setting2 = CreateSetting("Zwei");
            var setting3 = CreateSetting("Drei");
            var settings = new List<ISetting>()
            {
                setting1,
                setting2,
                setting3,
            };

            var returned = SettingFilter.GetSettingByName(settings, "Zwei");

            returned.Should().NotBeNull();
            returned.Should().BeSameAs(setting2);
        }

        [Fact]
        public void ReturnsNullIfNotExisting()
        {
            var setting1 = CreateSetting("Eins");
            var setting2 = CreateSetting("Zwei");
            var setting3 = CreateSetting("Drei");
            var settings = new List<ISetting>()
            {
                setting1,
                setting2,
                setting3,
            };

            var returned = SettingFilter.GetSettingByName(settings, "Vier");

            returned.Should().BeNull();
        }

        private static ISetting CreateSetting(string name)
        {
            var setting = Substitute.For<ISetting>();
            setting.Design.Name.Returns(name);
            return setting;
        }
    }

    public class GetSettingById
    {
        [Fact]
        public void ReturnsCorrectSettingIfExisting()
        {
            var setting1 = CreateSetting(Guid.Parse("927391f0-4474-4fbf-bf84-c075d03fde4c"));
            var setting2 = CreateSetting(Guid.Parse("b6646bdb-8910-48af-83c1-49678818a12f"));
            var setting3 = CreateSetting(Guid.Parse("4041e9a9-6f73-4eb2-9dda-f1a12960bb05"));
            var settings = new List<ISetting>()
            {
                setting1,
                setting2,
                setting3,
            };

            var returned = SettingFilter.GetSettingById(settings, Guid.Parse("b6646bdb-8910-48af-83c1-49678818a12f"));

            returned.Should().NotBeNull();
            returned.Should().BeSameAs(setting2);
        }

        [Fact]
        public void ReturnsNullIfNotExisting()
        {
            var setting1 = CreateSetting(Guid.Parse("927391f0-4474-4fbf-bf84-c075d03fde4c"));
            var setting2 = CreateSetting(Guid.Parse("b6646bdb-8910-48af-83c1-49678818a12f"));
            var setting3 = CreateSetting(Guid.Parse("4041e9a9-6f73-4eb2-9dda-f1a12960bb05"));
            var settings = new List<ISetting>()
            {
                setting1,
                setting2,
                setting3,
            };

            var returned = SettingFilter.GetSettingById(settings, Guid.Parse("6f3b2e7c-4f33-4df7-ac6a-9e1a4ddcb14c"));

            returned.Should().BeNull();
        }

        private static ISetting CreateSetting(Guid id)
        {
            var setting = Substitute.For<ISetting>();
            setting.Id.Returns(id);
            return setting;
        }
    }
}
