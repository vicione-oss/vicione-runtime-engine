namespace ViciOne.Core.Dataflow.DataModel.Editors;

public interface ISettingDesignEditor
{
    FunctionBlockDesignEditor? FunctionBlockDesignEditor { get; set; }
    SettingDesign Setting { get; }
}
