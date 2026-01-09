using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;

namespace Demo;

[FunctionBlockDesign("3db9c2b3-d076-4099-85a5-79b2c9418399", "8706f9c5-3185-42bd-be7c-ae4d1b20b71d", "a989cc30-31f6-40dd-bab6-e6cd988e0b96", Namespace = "Demo.With.Driver")]
[DefaultCycleFrequency(50)]
[ValidRunModes(RunMode.Cyclic)]
[DefaultRunMode(RunMode.Cyclic)]
[Driver<DemoDriver>]
public partial class DriverDemo : RuntimeFunctionBlock<DriverDemo>
{
    public new DemoDriver Driver => (DemoDriver)base.Driver;

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("c1471b1b-5f6c-4b7e-9a61-59ab6df9e0d9")]
        [DateTimeDefaultValue(2000, 01, 01, 06, 30, 00)]
        public IRuntimeConnector<DateTime> Time { get; internal set; }
        [OutputConnectorDesign("76656482-23ee-475d-bf7a-6e5c3a981ffb")]
        public IRuntimeConnector<bool> Toggle { get; internal set; }
    }
}
