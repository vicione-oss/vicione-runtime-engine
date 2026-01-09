using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class FunctionBlockDesignGeneratorTests
{
    public class GenerateFunctionBlockDesigns
    {
        [Fact]
        public void GeneratesAllDesigns()
        {
            var designs = FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(
            [
                CreateInfoWithAllDetails(),
                CreateInfoWithOnlyNecessaryDetails(),
                CreateInfoWithDriver(),
            ], CreatePoolingFactory());

            designs.Should().HaveCount(3);
        }

        [Fact]
        public void GetsCorrectNames()
        {
            var info1 = CreateInfoWithAllDetails();
            var info2 = CreateInfoWithOnlyNecessaryDetails();
            var info3 = CreateInfoWithDriver();

            var designs = FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(
            [
                info1,
                info2,
                info3,
            ], CreatePoolingFactory());

            designs.Where(x => x.Name == info1.Name).Should().HaveCount(1);
            designs.Where(x => x.Name == info2.Name).Should().HaveCount(1);
            designs.Where(x => x.Name == info3.Name).Should().HaveCount(1);
        }
    }

    public class GenerateFunctionBlockDesign
    {
        [Fact]
        public void GetsCorrectId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                 info, CreatePoolingFactory());

            design.Id.Should().Be(info.Id);
        }

        [Fact]
        public void GetsCorrectEnabledInputId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                 info, CreatePoolingFactory());

            design.SystemInputs.First(x => x.Name == SystemConnectorNames.FunctionBlockEnabledName).Id.Should().Be(info.FunctionBlockEnabledInputId);
        }

        [Fact]
        public void GetsCorrectEnabledOutputId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                 info, CreatePoolingFactory());

            design.SystemOutputs.First(x => x.Name == SystemConnectorNames.FunctionBlockEnabledName).Id.Should().Be(info.FunctionBlockEnabledOutputId);
        }

        [Fact]
        public void GetsCorrectNamespace()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Namespace.Should().Be(info.Namespace);
        }

        [Fact]
        public void GetsCorrectDefaultCycleFrequencyIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.DefaultCycleFrequency.Should().Be(info.DefaultCycleFrequency);
        }

        [Fact]
        public void CanHandleDefaultCycleFrequencyIsNull()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Should().NotBeNull();
        }

        [Fact]
        public void GetsCorrectDriverIfSet()
        {
            var info = CreateInfoWithDriver();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.RuntimeDriverTypeName.Should().Be(info.RuntimeDriverTypeName);
        }

        [Fact]
        public void CanHandleDriverIsNull()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Should().NotBeNull();
            design.RuntimeDriverTypeName.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectValidRunModesIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ValidRunModes.Should().ContainAllButNotMore(info.ValidRunModes!);
        }

        [Fact]
        public void CanHandleValidRunModesIsNull()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ValidRunModes.Should().NotBeEmpty();
        }

        [Fact]
        public void GetsCorrectDefaultRunModeIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.DefaultRunMode.Should().Be(info.DefaultRunMode);
        }

        [Fact]
        public void CanHandleDefaultRunModeIsNull()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Should().NotBeNull();
        }

        public static TheoryData<FunctionBlockDesignInfo> ArgumentsFor_GetsAll_ => new()
        {
            { CreateInfoWithAllDetails() },
            { CreateInfoWithOnlyNecessaryDetails() },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsAll_))]
        public void GetsAllInputs(FunctionBlockDesignInfo info)
        {
            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataInputs.Should().HaveCount(info.Inputs.Count);
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsAll_))]
        public void GetsAllOutputs(FunctionBlockDesignInfo info)
        {
            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataOutputs.Should().HaveCount(info.Outputs.Count);
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsAll_))]
        public void GetsAllSettings(FunctionBlockDesignInfo info)
        {
            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.Should().HaveCount(info.Settings.Count);
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsAll_))]
        public void GetsAllPersistentVariables(FunctionBlockDesignInfo info)
        {
            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.PersistentVariables.Should().HaveCount(info.PersistentVariables.Count);
        }

        [Fact]
        public void GetsCorrectInputId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataInputs.First().Id.Should().Be(info.Inputs.First().Id);
        }

        [Fact]
        public void GetsCorrectOutputId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataOutputs.First().Id.Should().Be(info.Outputs.First().Id);
        }

        [Fact]
        public void GetsCorrectSettingId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.First().Id.Should().Be(info.Settings.First().Id);
        }

        [Fact]
        public void GetsCorrectSettingInputId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.First().Input.Id.Should().Be(info.Settings.First().InputId);
        }

        [Fact]
        public void GetsCorrectSettingOutputId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.First().Output.Id.Should().Be(info.Settings.First().OutputId);
        }

        [Fact]
        public void GetsCorrectPersistentVariableId()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.PersistentVariables.First().Id.Should().Be(info.PersistentVariables.First().Id);
        }

        [Fact]
        public void GetsCorrectInputName()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataInputs.First().Name.Should().Be(info.Inputs.First().Name);
        }

        [Fact]
        public void GetsCorrectOutputName()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataOutputs.First().Name.Should().Be(info.Outputs.First().Name);
        }

        [Fact]
        public void GetsCorrectSettingName()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.First().Name.Should().Be(info.Settings.First().Name);
        }

        [Fact]
        public void GetsCorrectPersistentVariableName()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.PersistentVariables.First().Name.Should().Be(info.PersistentVariables.First().Name);
        }

        [Fact]
        public void GetsCorrectInputType()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataInputs.First().Should().BeAssignableTo<IConnectorDesign<int>>();
        }

        [Fact]
        public void GetsCorrectOutputType()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataOutputs.First().Should().BeAssignableTo<IConnectorDesign<double>>();
        }

        [Fact]
        public void GetsCorrectSettingType()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.First().Should().BeAssignableTo<ISettingDesign<bool>>();
        }

        [Fact]
        public void GetsCorrectPersistentVariableType()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.PersistentVariables.First().Should().BeAssignableTo<IPersistentVariableDesign<bool>>();
        }

        [Fact]
        public void GetsCorrectDefaultCrossSourcePoolingStrategyIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignInput<int>)design.ProcessDataInputs.First()).DefaultCrossSourcePoolingStrategy.Should().Be(
                info.Inputs.First().DefaultCrossSourcePoolingStrategy);
        }

        [Fact]
        public void GetsCorrectDefaultCrossSourceAggregatingPoolingIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignInput<int>)design.ProcessDataInputs.First()).DefaultCrossSourceAggregatingPooling.Should().BeSameAs(
                info.Inputs.First().DefaultCrossSourceAggregatingPooling);
        }

        [Fact]
        public void GetsCorrectDefaultPerSourcePoolingStrategyIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignInput<int>)design.ProcessDataInputs.First()).DefaultPerSourcePoolingStrategy.Should().Be(
                info.Inputs.First().DefaultPerSourcePoolingStrategy);
        }

        [Fact]
        public void GetsCorrectDefaultPerSourceAggregatingPoolingIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignInput<int>)design.ProcessDataInputs.First()).DefaultPerSourceAggregatingPooling.Should().BeSameAs(
                info.Inputs.First().DefaultPerSourceAggregatingPooling);
        }

        [Fact]
        public void GetsCorrectInputDefaultValueIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataInputs.First().DefaultValue.Should().Be(info.Inputs.First().DefaultValue);
        }

        [Fact]
        public void GetsCorrectInputDefaultValueIfNotSet()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataInputs.First().DefaultValue.Should().Be(default(bool));
        }

        [Fact]
        public void GetsCorrectOutputDefaultValueIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataOutputs.First().DefaultValue.Should().Be(info.Outputs.First().DefaultValue);
        }

        [Fact]
        public void GetsCorrectOutputDefaultValueIfNotSet()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.ProcessDataOutputs.First().DefaultValue.Should().Be(default(bool));
        }

        [Fact]
        public void GetsCorrectSettingDefaultValueIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.First().DefaultValue.Should().Be(info.Settings.First().DefaultValue);
        }

        [Fact]
        public void GetsCorrectSettingDefaultValueIfNotSet()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.Settings.First().DefaultValue.Should().Be(default(bool));
        }

        [Fact]
        public void GetsCorrectPersistentVariableDefaultValueIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.PersistentVariables.First().DefaultValue.Should().Be(info.PersistentVariables.First().DefaultValue);
        }

        [Fact]
        public void GetsCorrectPersistentVariableDefaultValueIfNotSet()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            design.PersistentVariables.First().DefaultValue.Should().Be(default(bool));
        }

        [Fact]
        public void GetsCorrectInputValidatorsIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignInput<int>)design.ProcessDataInputs.First()).Validators.Should().BeEquivalentTo(
                 info.Inputs.First().Validators);
        }

        [Fact]
        public void GetsCorrectInputValidatorsIfNotSet()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignInput<bool>)design.ProcessDataInputs.First()).Validators.Should().BeEmpty();
        }

        [Fact]
        public void GetsCorrectOutputValidatorsIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignOutput<double>)design.ProcessDataOutputs.First()).Validators.Should().BeEquivalentTo(
                 info.Outputs.First().Validators);
        }

        [Fact]
        public void GetsCorrectOutputValidatorsIfNotSet()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((ConnectorDesignOutput<bool>)design.ProcessDataOutputs.First()).Validators.Should().BeEmpty();
        }

        [Fact]
        public void GetsCorrectSettingValidatorsIfSet()
        {
            var info = CreateInfoWithAllDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((SettingDesign<bool>)design.Settings.First()).Validators.Should().BeEquivalentTo(
                 info.Settings.First().Validators);
        }

        [Fact]
        public void GetsCorrectSettingValidatorsIfNotSet()
        {
            var info = CreateInfoWithOnlyNecessaryDetails();

            var design = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(
                info, CreatePoolingFactory());

            ((SettingDesign<bool>)design.Settings.First()).Validators.Should().BeEmpty();
        }
    }

    private static FunctionBlockDesignInfo CreateInfoWithAllDetails()
    {
        var info = new FunctionBlockDesignInfo()
        {
            DefaultCycleFrequency = 42,
            DefaultRunMode = RunMode.Cyclic,
            Name = "DummyInfo1",
            Namespace = "DummyNamespace1",
            ValidRunModes = [RunMode.Cyclic],
        };

        info.Inputs.Add(new ConnectorDesignInfo
        {
            DefaultCrossSourcePoolingStrategy = PoolingStrategy.Aggregate,
            DefaultCrossSourceAggregatingPooling = new DummyPooling<int>(),
            DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue,
            DefaultPerSourceAggregatingPooling = null,
            DefaultValue = 42,
            Name = "DummyIn",
            Type = typeof(int),
            Validators = [new DummyValidator<int>()],
        });

        info.Outputs.Add(new ConnectorDesignInfo
        {
            DefaultValue = 66.6,
            Name = "DummyOut",
            Type = typeof(double),
            Validators = [new DummyValidator<double>()],
        });

        info.Settings.Add(new SettingDesignInfo
        {
            DefaultValue = true,
            Name = "DummySetting",
            Type = typeof(bool),
            Validators = [new DummyValidator<bool>()],
        });

        info.PersistentVariables.Add(new PersistentVariableDesignInfo
        {
            DefaultValue = true,
            Name = "DummyPersistentVariable",
            Type = typeof(bool),
        });

        return info;
    }

    private static FunctionBlockDesignInfo CreateInfoWithOnlyNecessaryDetails()
    {
        var info = new FunctionBlockDesignInfo()
        {
            Name = "DummyInfo2",
            Namespace = "DummyNamespace2",
        };

        info.Inputs.Add(new ConnectorDesignInfo
        {
            Name = "DummyIn1",
            Type = typeof(bool),
        });

        info.Inputs.Add(new ConnectorDesignInfo
        {
            Name = "DummyIn2",
            Type = typeof(bool),
        });

        info.Outputs.Add(new ConnectorDesignInfo
        {
            Name = "DummyOut1",
            Type = typeof(bool),
        });

        info.Outputs.Add(new ConnectorDesignInfo
        {
            Name = "DummyOut2",
            Type = typeof(bool),
        });

        info.Outputs.Add(new ConnectorDesignInfo
        {
            Name = "DummyOut3",
            Type = typeof(bool),
        });

        info.Settings.Add(new SettingDesignInfo
        {
            Name = "DummySetting1",
            Type = typeof(bool),
        });

        info.Settings.Add(new SettingDesignInfo
        {
            Name = "DummySetting2",
            Type = typeof(bool),
        });

        info.Settings.Add(new SettingDesignInfo
        {
            Name = "DummySetting3",
            Type = typeof(bool),
        });

        info.Settings.Add(new SettingDesignInfo
        {
            Name = "DummySetting4",
            Type = typeof(bool),
        });

        info.PersistentVariables.Add(new PersistentVariableDesignInfo
        {
            Name = "DummyPersistentVariable1",
            Type = typeof(bool),
        });

        info.PersistentVariables.Add(new PersistentVariableDesignInfo
        {
            Name = "DummyPersistentVariable2",
            Type = typeof(bool),
        });

        info.PersistentVariables.Add(new PersistentVariableDesignInfo
        {
            Name = "DummyPersistentVariable3",
            Type = typeof(bool),
        });

        info.PersistentVariables.Add(new PersistentVariableDesignInfo
        {
            Name = "DummyPersistentVariable4",
            Type = typeof(bool),
        });

        return info;
    }

    private static FunctionBlockDesignInfo CreateInfoWithDriver()
    {
        var info = new FunctionBlockDesignInfo()
        {
            Name = "DummyInfo3",
            Namespace = "DummyNamespace3",
            RuntimeDriverTypeName = "DriverType",
        };

        return info;
    }

    private static PoolingFactory CreatePoolingFactory()
        => new([Assembly.GetExecutingAssembly()]);

    private sealed class DummyValidator<TValue> : IValidator<TValue>
    {
        public TValue? Validate(TValue? value, bool throwIfInvalid) => throw new NotImplementedException();
        object? IValidator.Validate(object? value, bool throwIfInvalid) => Validate((TValue?)value, throwIfInvalid);
    }

    public sealed class DummyPooling<TValue> : IAggregatingPooling<TValue>
    {
        public string Name => string.Empty;
        public string Description => string.Empty;

        public TValue? Aggregate(IEnumerable<TValue?> values) => throw new NotImplementedException();
    }
}
