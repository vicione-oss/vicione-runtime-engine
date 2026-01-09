using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public static class SettingFactory
{
    public static Setting DesignToSetting(SettingDesign design)
    {
        var editor = (ISettingEditor)Activator.CreateInstance(design.SettingEditorType, [design])!;
        return editor.Setting;
    }
}
