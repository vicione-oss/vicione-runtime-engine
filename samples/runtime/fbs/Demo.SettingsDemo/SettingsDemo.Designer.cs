using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;

namespace Demo;

[FunctionBlockDesign("510e02ee-b5fc-4625-8fda-37e499f5f1a1", "ad1faf95-b874-4393-9070-d03bb077fb87", "4c6e98b4-662d-4fda-bcb5-19a99afffa3c")]
[ValidRunModes(RunMode.Cyclic)]
[DefaultRunMode(RunMode.Cyclic)]
[DefaultCycleFrequency(1)]
public partial class SettingsDemo : RuntimeFunctionBlock<SettingsDemo>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("914c4a77-d907-4543-9ff9-0610c16ef49e")]
        [DefaultCrossSourcePoolingStrategy(PoolingStrategy.Queue)]
        [RangeValidator<double>(-100.0, 100.0)]
        [InequalityValidator<double>(0.0, 1.0)]
        public IRuntimeConnector<double> DummyForDefaultPooling1 { get; internal set; }
        [InputConnectorDesign("c480efa8-c607-4360-b3ff-cd8ce0d7f8ef")]
        [DefaultCrossSourcePoolingStrategy(PoolingStrategy.Concat)]
        public IRuntimeConnector<double[]> DummyForDefaultPooling2 { get; internal set; }
        [InputConnectorDesign("b6d86880-58f2-477c-9a87-c4a5f9968c63")]
        [DefaultCrossSourcePoolingStrategy(PoolingStrategy.Aggregate)]
        [DefaultCrossSourceAggregatingPooling<OrPooling>()]
        [DefaultPerSourcePoolingStrategy(PoolingStrategy.Queue)]
        public IRuntimeConnector<bool> DummyForDefaultPooling3 { get; internal set; }
    }

    public new Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_
    {
        [SettingDesign("6d374eec-12ed-4ae2-8391-5fd3279dba89", "d9bccf18-fadf-4be6-ae02-9c4c9c65edac", "c909aa0b-b7b4-4009-b385-fbc37c0bed1b")]
        [DefaultValue(true)]
        public IRuntimeSetting<bool> DummySetting1 { get; internal set; }
        [SettingDesign("902dd1a1-997f-4bce-846e-e47c959965f8", "5f1bfa66-fb39-437d-bf04-07f27de4e2f5", "8cf54142-db9e-4bda-9e49-4b3966aea5b1")]
        [EnumValidator<DummyValues>(DummyValues.First)]
        [DefaultValue(DummyValues.First)]
        public IRuntimeSetting<DummyValues> DummySetting2 { get; internal set; }
    }
}
