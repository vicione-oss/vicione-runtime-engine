using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.Core.Contracts.DataModel.Filters;

public static class SettingFilter
{
    public static ISetting? GetSettingByName(IEnumerable<ISetting> settings, string name)
        => settings.FirstOrDefault(x => string.Equals(x.Design.Name, name, StringComparison.OrdinalIgnoreCase));

    public static ISetting? GetSettingById(IEnumerable<ISetting> settings, Guid id)
        => settings.FirstOrDefault(x => x.Id == id);
}
