using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Stubs;

internal sealed class EmptySettingDesign : SettingDesign
{
    public static EmptySettingDesign Default { get; } = new();

    public override Type SettingEditorType => typeof(object);
    public override Type SettingType => typeof(object);

    private protected override IEnumerable<IValidator> GetValidators() => [];
}
