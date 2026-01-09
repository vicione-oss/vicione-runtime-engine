using ViciOne.Core.Contracts.Runtime;

namespace Demo;

public partial class ComplexTypeDemo
{
    private int _lastNumber;

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (ProcessDataInputs.In.Changed)
        {
            Base? inValue = ProcessDataInputs.In.Value;
            if (inValue is not null)
            {
                ProcessDataOutputs.InIsNotNull.SetValueIfNotEqual(true);
                ProcessDataOutputs.InNumericValue.SetValueIfNotEqual(inValue.NumericValue);
                ProcessDataOutputs.InNumericValue.SetValidityIfNotEqual(ViciOne.Core.Contracts.DataModel.Validity.Valid);
                if (inValue is Derived derivedInValue)
                {
                    ProcessDataOutputs.InIsDerived.SetValueIfNotEqual(true);
                    ProcessDataOutputs.InStringValue.SetValueIfNotEqual(derivedInValue.StringValue);
                    ProcessDataOutputs.InStringValue.SetValidityIfNotEqual(ViciOne.Core.Contracts.DataModel.Validity.Valid);
                }
                else
                {
                    ProcessDataOutputs.InIsDerived.SetValueIfNotEqual(false);
                    ProcessDataOutputs.InStringValue.SetValidityIfNotEqual(ViciOne.Core.Contracts.DataModel.Validity.Invalid);
                }
            }
            else
            {
                ProcessDataOutputs.InIsNotNull.SetValueIfNotEqual(false);
                ProcessDataOutputs.InNumericValue.SetValidityIfNotEqual(ViciOne.Core.Contracts.DataModel.Validity.Invalid);
                ProcessDataOutputs.InStringValue.SetValidityIfNotEqual(ViciOne.Core.Contracts.DataModel.Validity.Invalid);
            }
        }
        if (ProcessDataInputs.GenerateBase.Changed && ProcessDataInputs.GenerateBase.Value)
            ProcessDataOutputs.GeneratedBase.SetValueIfNotEqual(GenerateBase());
        if (ProcessDataInputs.GenerateDerived.Changed && ProcessDataInputs.GenerateDerived.Value)
            ProcessDataOutputs.GeneratedDerived.SetValueIfNotEqual(GenerateDerived());
    }

    private Base GenerateBase()
        => new()
        {
            NumericValue = _lastNumber++,
        };

    private Derived GenerateDerived()
        => new()
        {
            NumericValue = _lastNumber++,
        };

    protected override void InitializeSettings()
    {
        base.InitializeSettings();
        _lastNumber = Settings.NumericValueOffset.Value;
    }
}
