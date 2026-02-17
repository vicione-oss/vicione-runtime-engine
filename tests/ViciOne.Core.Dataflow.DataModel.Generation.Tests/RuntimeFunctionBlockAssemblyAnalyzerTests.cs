using DummyBlocksForAnalyzer;
using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Runtime.Attributes;
using Xunit;

public class RuntimeFunctionBlockAssemblyAnalyzerTests
{
    public class GetFunctionBlockDesigns
    {
        [Fact]
        public void GetsAllDesigns()
        {
            var designs = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesigns([Assembly.GetExecutingAssembly()]).ToList();

            designs.Should().HaveCount(6);
        }
    }

    public class GetFunctionBlockDesign
    {
        [Fact]
        public void GetsCorrectId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Id.Should().Be(DummyRuntimeBlock1.Id);
        }

        [Fact]
        public void GetsCorrectFunctionBlockEnabledInputId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.FunctionBlockEnabledInputId.Should().Be(DummyRuntimeBlock1.FunctionBlockEnabledInputId);
        }

        [Fact]
        public void GetsCorrectFunctionBlockEnabledOutputId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.FunctionBlockEnabledOutputId.Should().Be(DummyRuntimeBlock1.FunctionBlockEnabledOutputId);
        }

        [Fact]
        public void UsesTypeNamespaceIfNotExplicitlySet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Namespace.Should().Be(typeof(DummyRuntimeBlock1).Namespace);
        }

        [Fact]
        public void UsesTypeNameIfNotExplicitlySet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Name.Should().Be(nameof(DummyRuntimeBlock1));
        }

        [Fact]
        public void UsesNamespaceIfExplicitlySet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlockWithExplicitNamespace));

            design.Namespace.Should().Be(DummyRuntimeBlockWithExplicitNamespace.ExplicitNamespace);
        }

        [Fact]
        public void UsesNameIfExplicitlySet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlockWithExplicitName));

            design.Name.Should().Be(DummyRuntimeBlockWithExplicitName.ExplicitName);
        }

        [Fact]
        public void GetsRuntimeTypeNameIfNamespaceIsExplicitlySet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlockWithExplicitNamespace));

            design.RuntimeTypeName.Should().Be(typeof(DummyRuntimeBlockWithExplicitNamespace).FullName);
        }

        [Fact]
        public void GetsRuntimeTypeNameIfNameIsExplicitlySet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlockWithExplicitName));

            design.RuntimeTypeName.Should().Be(typeof(DummyRuntimeBlockWithExplicitName).FullName);
        }

        [Fact]
        public void DoesNotGetRuntimeTypeNameIfNamespaceAndNameIsNotExplicitlySet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.RuntimeTypeName.Should().BeNull();
        }

        [Fact]
        public void CanHandleAssemblyFileNameDeterminationError()
        {
            var dynamicType = CreateDynamicRuntimeFunctionBlockType();

            Action action = () => RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(dynamicType);

            action.Should().NotThrow();
        }

        private static Type CreateDynamicRuntimeFunctionBlockType()
        {
            var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(Guid.NewGuid().ToString());
            var typeBuilder = moduleBuilder.DefineType(Guid.NewGuid().ToString(), TypeAttributes.Public);
            var attributeConstructorInfo = typeof(FunctionBlockDesignAttribute).GetConstructor([typeof(string), typeof(string), typeof(string)]);
            var attributeBuilder = new CustomAttributeBuilder(attributeConstructorInfo!,
            [
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
            ]);
            typeBuilder.SetCustomAttribute(attributeBuilder);
            return typeBuilder.CreateType()!;
        }

        [Fact]
        public void GetsCorrectDefaultCycleFrequencyIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.DefaultCycleFrequency.Should().Be(DummyRuntimeBlock1.DefaultCycleFrequency);
        }

        [Fact]
        public void GetsNullIfDefaultCycleFrequencyIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.DefaultCycleFrequency.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectDriverIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock4));

            design.RuntimeDriverTypeName.Should().Be(DummyRuntimeBlock4.DriverTypeName);
        }

        [Fact]
        public void GetsNullIfDriverIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.RuntimeDriverTypeName.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectValidRunModesIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.ValidRunModes.Should()
                .ContainSingle()
                    .Which.Should()
                        .Be(DummyRuntimeBlock1.ValidRunMode);
        }

        [Fact]
        public void GetsNullIfValidRunModesAreNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.ValidRunModes.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectDefaultRunModeIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.DefaultRunMode.Should().Be(DummyRuntimeBlock1.DefaultRunMode);
        }

        [Fact]
        public void GetsNullIfDefaultRunModeIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.DefaultRunMode.Should().BeNull();
        }

        [Theory]
        [InlineData(typeof(DummyRuntimeBlock1), 1)]
        [InlineData(typeof(DummyRuntimeBlock2), 2)]
        [InlineData(typeof(DummyRuntimeBlock3), 0)]
        public void GetsAllInputs(Type blockType, int expectedCount)
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(blockType);

            design.Inputs.Should().HaveCount(expectedCount);
        }

        [Theory]
        [InlineData(typeof(DummyRuntimeBlock1), 1)]
        [InlineData(typeof(DummyRuntimeBlock2), 3)]
        [InlineData(typeof(DummyRuntimeBlock3), 0)]
        public void GetsAllOutputs(Type blockType, int expectedCount)
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(blockType);

            design.Outputs.Should().HaveCount(expectedCount);
        }

        [Theory]
        [InlineData(typeof(DummyRuntimeBlock1), 1)]
        [InlineData(typeof(DummyRuntimeBlock2), 4)]
        [InlineData(typeof(DummyRuntimeBlock3), 0)]
        public void GetsAllSettings(Type blockType, int expectedCount)
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(blockType);

            design.Settings.Should().HaveCount(expectedCount);
        }

        [Theory]
        [InlineData(typeof(DummyRuntimeBlock1), 1)]
        [InlineData(typeof(DummyRuntimeBlock2), 5)]
        [InlineData(typeof(DummyRuntimeBlock3), 0)]
        public void GetsAllPersistentVariables(Type blockType, int expectedCount)
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(blockType);

            design.PersistentVariables.Should().HaveCount(expectedCount);
        }

        [Fact]
        public void GetsCorrectInputId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().Id.Should().Be(DummyRuntimeBlock1.Inputs.DummyInId);
        }

        [Fact]
        public void GetsCorrectOutputId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Outputs.First().Id.Should().Be(DummyRuntimeBlock1.Outputs.DummyOutId);
        }

        [Fact]
        public void GetsCorrectSettingId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Settings.First().Id.Should().Be(DummyRuntimeBlock1.Settings.DummySettingId);
        }

        [Fact]
        public void GetsCorrectSettingInputId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Settings.First().InputId.Should().Be(DummyRuntimeBlock1.Settings.DummySettingInputId);
        }

        [Fact]
        public void GetsCorrectSettingOutputId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Settings.First().OutputId.Should().Be(DummyRuntimeBlock1.Settings.DummySettingOutputId);
        }

        [Fact]
        public void GetsCorrectPersistentVariableId()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.PersistentVariables.First().Id.Should().Be(DummyRuntimeBlock1.PersistentVariables.DummyPersistentVariableId);
        }

        [Fact]
        public void GetsCorrectInputName()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().Name.Should().Be(nameof(DummyRuntimeBlock1.Inputs.DummyIn));
        }

        [Fact]
        public void GetsCorrectOutputName()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Outputs.First().Name.Should().Be(nameof(DummyRuntimeBlock1.Outputs.DummyOut));
        }

        [Fact]
        public void GetsCorrectSettingName()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Settings.First().Name.Should().Be(nameof(DummyRuntimeBlock1.Settings.DummySetting));
        }

        [Fact]
        public void GetsCorrectPersistentVariableName()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.PersistentVariables.First().Name.Should().Be(nameof(DummyRuntimeBlock1.PersistentVariables.DummyPersistentVariable));
        }

        [Fact]
        public void GetsCorrectInputType()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().Type.Should().Be<int>();
        }

        [Fact]
        public void GetsCorrectOutputType()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Outputs.First().Type.Should().Be<double>();
        }

        [Fact]
        public void GetsCorrectSettingType()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Settings.First().Type.Should().Be<bool>();
        }

        [Fact]
        public void GetsCorrectPersistentVariableType()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.PersistentVariables.First().Type.Should().Be<bool>();
        }

        [Fact]
        public void GetsCorrectDefaultCrossSourcePoolingStrategyIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().DefaultCrossSourcePoolingStrategy.Should().Be(PoolingStrategy.Aggregate);
        }

        [Fact]
        public void GetsNullIfDefaultCrossSourcePoolingStrategyIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Inputs.First().DefaultCrossSourcePoolingStrategy.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectDefaultCrossSourceAggregatingPoolingIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().DefaultCrossSourceAggregatingPooling.Should().BeOfType<DummyPooling1>();
        }

        [Fact]
        public void GetsNullIfDefaultCrossSourceAggregatingPoolingIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Inputs.First().DefaultCrossSourceAggregatingPooling.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectDefaultPerSourcePoolingStrategyIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().DefaultPerSourcePoolingStrategy.Should().Be(PoolingStrategy.Aggregate);
        }

        [Fact]
        public void GetsNullIfDefaultPerSourcePoolingStrategyIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Inputs.First().DefaultPerSourcePoolingStrategy.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectDefaultPerSourceAggregatingPoolingIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().DefaultPerSourceAggregatingPooling.Should().BeOfType<DummyPooling2>();
        }

        [Fact]
        public void GetsNullIfDefaultPerSourceAggregatingPoolingIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Inputs.First().DefaultPerSourceAggregatingPooling.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectInputDefaultValueIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().DefaultValue.Should().Be(DummyRuntimeBlock1.Inputs.DummyInDefaultValue);
        }

        [Fact]
        public void GetsNullIfInputDefaultValueIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Inputs.First().DefaultValue.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectOutputDefaultValueIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Outputs.First().DefaultValue.Should().Be(DummyRuntimeBlock1.Outputs.DummyOutDefaultValue);
        }

        [Fact]
        public void GetsNullIfOutputDefaultValueIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Outputs.First().DefaultValue.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectSettingDefaultValueIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Settings.First().DefaultValue.Should().Be(DummyRuntimeBlock1.Settings.DummySettingDefaultValue);
        }

        [Fact]
        public void GetsNullIfSettingDefaultValueIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Settings.First().DefaultValue.Should().BeNull();
        }

        [Fact]
        public void GetsCorrectPersistentVariableDefaultValueIfSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.PersistentVariables.First().DefaultValue.Should().Be(DummyRuntimeBlock1.PersistentVariables.DummyPersistentVariableDefaultValue);
        }

        [Fact]
        public void GetsNullIfPersistentVariableDefaultValueIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.PersistentVariables.First().DefaultValue.Should().BeNull();
        }

        [Fact]
        public void GetsInputValidatorsAccordingAttributeIfOnlyOneSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Inputs.First().Validators.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeOfType<DummyValidator<int>>();
        }

        [Fact]
        public void GetsInputValidatorsAccordingAttributesIfMultipleSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Inputs.Last().Validators.Should()
                .HaveCount(2).And
                .AllBeOfType<DummyValidator<bool>>();
        }

        [Fact]
        public void GetsEmptyListIfInputValidatorIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Inputs.First().Validators.Should().BeEmpty();
        }

        [Fact]
        public void GetsOutputValidatorAccordingAttributeIfOnlyOneSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Outputs.First().Validators.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeOfType<DummyValidator<double>>();
        }

        [Fact]
        public void GetsOutputValidatorsAccordingAttributesIfMultipleSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Outputs.Last().Validators.Should()
                .HaveCount(3).And
                .AllBeOfType<DummyValidator<bool>>();
        }

        [Fact]
        public void GetsEmptyListIfOutputValidatorIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Outputs.First().Validators.Should().BeEmpty();
        }

        [Fact]
        public void GetsSettingValidatorsAccordingAttributeIfOnlyOneSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock1));

            design.Settings.First().Validators.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeOfType<DummyValidator<bool>>();
        }

        [Fact]
        public void GetsSettingValidatorsAccordingAttributesIfMultipleSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Settings.Last().Validators.Should()
                .HaveCount(4).And
                .AllBeOfType<DummyValidator<bool>>();
        }

        [Fact]
        public void GetsEmptyListIfSettingValidatorIsNotSet()
        {
            var design = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(typeof(DummyRuntimeBlock2));

            design.Settings.First().Validators.Should().BeEmpty();
        }
    }
}

namespace DummyBlocksForAnalyzer
{
    [FunctionBlockDesign(Id, FunctionBlockEnabledInputId, FunctionBlockEnabledOutputId)]
    [ValidRunModes([ValidRunMode])]
    [DefaultRunMode(DefaultRunMode)]
    [DefaultCycleFrequency(DefaultCycleFrequency)]
    public class DummyRuntimeBlock1
    {
        public const string Id = "0e2abf06-286a-4dcc-b993-f18b5c7295b5";
        public const string FunctionBlockEnabledInputId = "0d919326-2cbe-4355-b534-fd8b73bf7d8e";
        public const string FunctionBlockEnabledOutputId = "a8b2842f-8215-4802-bd42-64fbe1faba6c";
        public const uint DefaultCycleFrequency = 42;
        public const RunMode DefaultRunMode = RunMode.Cyclic;
        public const RunMode ValidRunMode = RunMode.Cyclic;

        [InputConnectorDesigns]
        public class Inputs
        {
            public const string DummyInId = "a58fae8a-56ec-4b0c-bc25-354528b58d27";
            public const int DummyInDefaultValue = 42;

            [InputConnectorDesign(DummyInId)]
            [DefaultValue(DummyInDefaultValue)]
            [DefaultCrossSourcePoolingStrategy(PoolingStrategy.Aggregate)]
            [DefaultCrossSourceAggregatingPooling<DummyPooling1>]
            [DefaultPerSourcePoolingStrategy(PoolingStrategy.Aggregate)]
            [DefaultPerSourceAggregatingPooling<DummyPooling2>]
            [DummyValidator<int>]
            public IRuntimeConnector<int>? DummyIn { get; }
        }

        [OutputConnectorDesigns]
        public class Outputs
        {
            public const string DummyOutId = "f95e3da1-2b66-43d9-a8e6-659f88a305f5";
            public const double DummyOutDefaultValue = 66.6;

            [OutputConnectorDesign(DummyOutId)]
            [DefaultValue(DummyOutDefaultValue)]
            [DummyValidator<double>]
            public IRuntimeConnector<double>? DummyOut { get; }
        }

        [SettingDesigns]
        public class Settings
        {
            public const string DummySettingId = "ebdbfd64-e9c7-4e37-a971-924af924f3b3";
            public const string DummySettingInputId = "7ea7e1d1-3819-44d0-8e81-bf1a54173445";
            public const string DummySettingOutputId = "3572b155-dd2f-42be-b9a1-00ce19a1e8fa";
            public const bool DummySettingDefaultValue = true;

            [SettingDesign(DummySettingId, DummySettingInputId, DummySettingOutputId)]
            [DefaultValue(true)]
            [DummyValidator<bool>]
            public IRuntimeSetting<bool>? DummySetting { get; internal set; }
        }

        [PersistentVariableDesigns]
        public class PersistentVariables
        {
            public const string DummyPersistentVariableId = "eb5b6e46-9b93-44f5-a09e-071e4364a866";
            public const bool DummyPersistentVariableDefaultValue = true;

            [PersistentVariableDesign(DummyPersistentVariableId)]
            [DefaultValue(true)]
            public IRuntimePersistentVariable<bool>? DummyPersistentVariable { get; internal set; }
        }
    }

    [FunctionBlockDesign(Id, EnabledInputId, EnabledOutputId)]
    public class DummyRuntimeBlock2
    {
        public const string Id = "1a6c2e07-9afa-4320-872e-1a02567b762f";
        public const string EnabledInputId = "b4684444-27aa-47b8-84ba-c2e5534a14ea";
        public const string EnabledOutputId = "7898158e-ebc6-4025-a7aa-4a80da847e5e";

        [InputConnectorDesigns]
        public class Inputs
        {
            public const string DummyIn1Id = "d203f9ca-99ba-40fa-8e25-635f44398df2";
            public const string DummyIn2Id = "8cf39f25-a8d9-48d6-8aaa-3070084fd276";

            [InputConnectorDesign(DummyIn1Id)]
            public IRuntimeConnector<bool>? DummyIn1 { get; }
            [InputConnectorDesign(DummyIn2Id)]
            [DummyValidator<bool>]
            [DummyValidator<bool>]
            public IRuntimeConnector<bool>? DummyIn2 { get; }
        }

        [OutputConnectorDesigns]
        public class Outputs
        {
            public const string DummyOut1Id = "5cf04199-5327-4136-b0d6-c855934e7941";
            public const string DummyOut2Id = "b9c7e284-2f5f-46af-bfe5-c2eeaf931fbb";
            public const string DummyOut3Id = "72708261-47a7-4604-8b3d-23fae6092bc8";

            [OutputConnectorDesign(DummyOut1Id)]
            public IRuntimeConnector<bool>? DummyOut1 { get; }
            [OutputConnectorDesign(DummyOut2Id)]
            public IRuntimeConnector<bool>? DummyOut2 { get; }
            [OutputConnectorDesign(DummyOut3Id)]
            [DummyValidator<bool>]
            [DummyValidator<bool>]
            [DummyValidator<bool>]
            public IRuntimeConnector<bool>? DummyOut3 { get; }
        }

        [SettingDesigns]
        public class Settings
        {
            public const string DummySetting1Id = "a9f3f396-d3ed-44ef-bafc-585c95094a4f";
            public const string DummySetting1InputId = "f684b5ed-a158-4ef9-a237-291dae353c5e";
            public const string DummySetting1OutputId = "ee96f086-c82f-4e3a-80ef-ed3e87d39555";
            public const string DummySetting2Id = "346a6dae-04ba-4385-b72a-5e9a5502d54d";
            public const string DummySetting2InputId = "be6d854a-11d2-4412-8ed8-c1d74edeac6f";
            public const string DummySetting2OutputId = "a6494ba1-15bb-4b10-b6d0-91ecb7ddc98c";
            public const string DummySetting3Id = "0fa30bf6-27a8-4690-bad8-2b5906562512";
            public const string DummySetting3InputId = "63db32b6-234d-4523-8592-0e18dfd3fb6d";
            public const string DummySetting3OutputId = "8db6fb88-b9aa-4de1-b77c-039f997cb7bd";
            public const string DummySetting4Id = "b6594410-e4f0-4a3e-b1dd-a94a23bbe37e";
            public const string DummySetting4InputId = "2112c008-4c17-42ba-bcc8-31132b2bc96b";
            public const string DummySetting4OutputId = "d3718594-af90-46f3-899b-4d8959eec57d";

            [SettingDesign(DummySetting1Id, DummySetting1InputId, DummySetting1OutputId)]
            public IRuntimeSetting<bool>? DummySetting1 { get; internal set; }
            [SettingDesign(DummySetting2Id, DummySetting2InputId, DummySetting2OutputId)]
            public IRuntimeSetting<bool>? DummySetting2 { get; internal set; }
            [SettingDesign(DummySetting3Id, DummySetting3InputId, DummySetting3OutputId)]
            public IRuntimeSetting<bool>? DummySetting3 { get; internal set; }
            [SettingDesign(DummySetting4Id, DummySetting4InputId, DummySetting4OutputId)]
            [DummyValidator<bool>]
            [DummyValidator<bool>]
            [DummyValidator<bool>]
            [DummyValidator<bool>]
            public IRuntimeSetting<bool>? DummySetting4 { get; internal set; }
        }

        [PersistentVariableDesigns]
        public class PersistentVariables
        {
            public const string DummyPersistentVariable1Id = "a9f3f396-d3ed-44ef-bafc-585c95094a4f";
            public const string DummyPersistentVariable2Id = "346a6dae-04ba-4385-b72a-5e9a5502d54d";
            public const string DummyPersistentVariable3Id = "0fa30bf6-27a8-4690-bad8-2b5906562512";
            public const string DummyPersistentVariable4Id = "b6594410-e4f0-4a3e-b1dd-a94a23bbe37e";
            public const string DummyPersistentVariable5Id = "cac6d403-3d45-4c07-9ab7-2467ff34bd40";

            [PersistentVariableDesign(DummyPersistentVariable1Id)]
            public IRuntimePersistentVariable<bool>? DummyPersistentVariable1 { get; internal set; }
            [PersistentVariableDesign(DummyPersistentVariable2Id)]
            public IRuntimePersistentVariable<bool>? DummyPersistentVariable2 { get; internal set; }
            [PersistentVariableDesign(DummyPersistentVariable3Id)]
            public IRuntimePersistentVariable<bool>? DummyPersistentVariable3 { get; internal set; }
            [PersistentVariableDesign(DummyPersistentVariable4Id)]
            public IRuntimePersistentVariable<bool>? DummyPersistentVariable4 { get; internal set; }
            [PersistentVariableDesign(DummyPersistentVariable5Id)]
            public IRuntimePersistentVariable<bool>? DummyPersistentVariable5 { get; internal set; }
        }
    }

    [FunctionBlockDesign(Id, EnabledInputId, EnabledOutputId)]
    public class DummyRuntimeBlock3
    {
        public const string Id = "53fc88f2-48fe-4db7-953f-080b23b55712";
        public const string EnabledInputId = "ddc262e7-02ef-4b2c-885c-e12dc73c9d5a";
        public const string EnabledOutputId = "9bdbfa3e-c6ce-4a04-916f-3cf752601bf5";
    }

    [FunctionBlockDesign(Id, EnabledInputId, EnabledOutputId)]
    [Driver(DriverTypeName)]
    public class DummyRuntimeBlock4
    {
        public const string Id = "7501399f-145c-4fcc-9034-9b563d560773";
        public const string EnabledInputId = "c1a8209e-099a-4243-9a92-a4a61b487b47";
        public const string EnabledOutputId = "2ec5352d-6534-4e86-8100-b2bc3fcc86ad";
        public const string DriverTypeName = "DriverType";
    }

    [FunctionBlockDesign(Id, EnabledInputId, EnabledOutputId, Namespace = ExplicitNamespace)]
    public class DummyRuntimeBlockWithExplicitNamespace
    {
        public const string Id = "1f03a4e0-e36f-4078-8c44-88a7c2fb6ba0";
        public const string EnabledInputId = "d514f5f4-fa37-4657-9f4d-8077b04b9689";
        public const string EnabledOutputId = "0e451307-d0fe-4909-a223-14aebf203a12";
        public const string ExplicitNamespace = "FortyTwo";
    }

    [FunctionBlockDesign(Id, EnabledInputId, EnabledOutputId, Name = ExplicitName)]
    public class DummyRuntimeBlockWithExplicitName
    {
        public const string Id = "29a593b9-3d2b-4d88-a693-f4e920f82238";
        public const string EnabledInputId = "74a3d5bf-5e9c-4cd5-96e1-fe82d9b5c365";
        public const string EnabledOutputId = "4acafebf-1426-4a15-926a-c6867a7174a7";
        public const string ExplicitName = "SixHundredSixtySix";
    }

    public class DummyValidator<TValue> : IValidator<TValue>
    {
        public TValue? Validate(TValue? value, bool throwIfInvalid) => throw new NotImplementedException();
        object? IValidator.Validate(object? value, bool throwIfInvalid) => Validate((TValue?)value, throwIfInvalid);
    }

    public class DummyValidatorAttribute<TValue> : ValidatorAttribute<TValue>
    {
        public override DummyValidator<TValue> Validator { get; } = new DummyValidator<TValue>();
    }

    public class DummyPooling1 : IAggregatingPooling<int>
    {
        public string Name => string.Empty;
        public string Description => string.Empty;

        public int Aggregate(IEnumerable<int> values) => throw new NotImplementedException();
    }

    public class DummyPooling2 : DummyPooling1 { }
}
