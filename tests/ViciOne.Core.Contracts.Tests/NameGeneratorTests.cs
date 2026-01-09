using AwesomeAssertions;
using NSubstitute;
using System;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using Xunit;

public class NameGeneratorTests
{
    public class GetUniqueName
    {
        public static TheoryData<IDataflow, IIdProvider, string> ArgumentsFor_CreatesCorrectUniqueNames
        {
            get
            {
                const string RootContainerName = "Root";
                const string RootContainerId = "4b67522d-4e5d-4406-8ad9-90523232e38c";
                const string Container1Name = "Container1";
                const string Container1Id = "217c3e46-aff8-43fc-9a3d-11efdda9edae";
                const string Container2Name = "Container2";
                const string Container2Id = "4878ed03-c549-48da-bdbf-a8ee28eb8eb0";
                const string Block1Name = "Block1";
                const string Block1Id = "135ea794-4553-479c-b302-4e151547b3cf";
                const string Block2Name = "Block2";
                const string Block2Id = "21cc0c6d-0254-4a35-8925-840c02d073a7";
                const string Setting1Name = "Setting1";
                const string Setting1Id = "ef80bfba-baa4-4841-9fbc-f6a3402f3f73";
                const string Setting2Name = "Setting2";
                const string Setting2Id = "fc076ad8-199c-4bee-8af2-08257f739660";
                const string SettingInputName = "SettingInput";
                const string SettingInputId = "55f84849-4a3f-4d35-983b-0253159ad4b2";
                const string SettingOutputName = "SettingOutput";
                const string SettingOutputId = "718def4f-b959-41ca-9082-4e99c9ab3176";
                const string SystemInputName = "SystemInput";
                const string SystemInputId = "81bb12a4-20f3-4122-969e-25bb314524c4";
                const string SystemOutputName = "SystemOutput";
                const string SystemOutputId = "24c97fe7-7aa5-4255-9331-520f8305278d";
                const string ProcessDataInputName = "ProcessDataInput";
                const string ProcessDataInputId = "4feb40ec-16f1-42d0-9a54-b5af3494c3b3";
                const string ProcessDataOutputName = "ProcessDataOutput";
                const string ProcessDataOutputId = "0f97ce9f-01cc-4082-aeb5-0b84ced6b3a4";

                var setting1 = Substitute.For<ISetting>();
                setting1.Design.Name.Returns(Setting1Name);
                setting1.Id.Returns(new Guid(Setting1Id));

                var setting2 = Substitute.For<ISetting>();
                setting2.Design.Name.Returns(Setting2Name);
                setting2.Id.Returns(new Guid(Setting2Id));

                var block1 = Substitute.For<IFunctionBlock>();
                block1.Name.Returns(Block1Name);
                block1.Id.Returns(new Guid(Block1Id));
                block1.Settings.Returns([setting1]);

                var block2 = Substitute.For<IFunctionBlock>();
                block2.Name.Returns(Block2Name);
                block2.Id.Returns(new Guid(Block2Id));
                block2.Settings.Returns([setting2]);

                var settingInput = Substitute.For<IConnector>();
                settingInput.Design.Name.Returns(SettingInputName);
                settingInput.Id.Returns(new Guid(SettingInputId));
                block2.SettingsInputs.Returns([settingInput]);

                var settingOutput = Substitute.For<IConnector>();
                settingOutput.Design.Name.Returns(SettingOutputName);
                settingOutput.Id.Returns(new Guid(SettingOutputId));
                block2.SettingsOutputs.Returns([settingOutput]);

                var systemInput = Substitute.For<IConnector>();
                systemInput.Design.Name.Returns(SystemInputName);
                systemInput.Id.Returns(new Guid(SystemInputId));
                block2.SystemInputs.Returns([systemInput]);

                var systemOutput = Substitute.For<IConnector>();
                systemOutput.Design.Name.Returns(SystemOutputName);
                systemOutput.Id.Returns(new Guid(SystemOutputId));
                block2.SystemOutputs.Returns([systemOutput]);

                var processDataInput = Substitute.For<IConnector>();
                processDataInput.Design.Name.Returns(ProcessDataInputName);
                processDataInput.Id.Returns(new Guid(ProcessDataInputId));
                block2.ProcessDataInputs.Returns([processDataInput]);

                var processDataOutput = Substitute.For<IConnector>();
                processDataOutput.Design.Name.Returns(ProcessDataOutputName);
                processDataOutput.Id.Returns(new Guid(ProcessDataOutputId));
                block2.ProcessDataOutputs.Returns([processDataOutput]);

                var container1 = Substitute.For<IContainer>();
                container1.Name.Returns(Container1Name);
                container1.Id.Returns(new Guid(Container1Id));

                var container2 = Substitute.For<IContainer>();
                container2.Name.Returns(Container2Name);
                container2.Id.Returns(new Guid(Container2Id));
                container2.FunctionBlocks.Returns([block2]);
                container1.Containers.Returns([container2]);

                var dataflow = Substitute.For<IDataflow>();
                dataflow.RootContainer.Name.Returns(RootContainerName);
                dataflow.RootContainer.Id.Returns(new Guid(RootContainerId));
                dataflow.RootContainer.Containers.Returns([container1]);
                dataflow.RootContainer.FunctionBlocks.Returns([block1]);

                var container1ExpectedUniqueName = $"{RootContainerName}{NameGenerator.NameSeparator}{Container1Name}";
                var container2ExpectedUniqueName = $"{RootContainerName}{NameGenerator.NameSeparator}{Container1Name}{NameGenerator.NameSeparator}{Container2Name}";
                var block1ExpectedUniqueName = $"{RootContainerName}{NameGenerator.NameSeparator}{Block1Name}";
                var block2ExpectedUniqueName = $"{RootContainerName}{NameGenerator.NameSeparator}{Container1Name}{NameGenerator.NameSeparator}{Container2Name}{NameGenerator.NameSeparator}{Block2Name}";

                return new()
                {
                    { dataflow, dataflow.RootContainer, RootContainerName },
                    { dataflow, container1, container1ExpectedUniqueName },
                    { dataflow, container2, container2ExpectedUniqueName },
                    { dataflow, block1, block1ExpectedUniqueName },
                    { dataflow, block2, block2ExpectedUniqueName },
                    { dataflow, setting1, $"{block1ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.SettingsIdentifier}{NameGenerator.NameSeparator}{Setting1Name}" },
                    { dataflow, setting2, $"{block2ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.SettingsIdentifier}{NameGenerator.NameSeparator}{Setting2Name}" },
                    { dataflow, settingInput, $"{block2ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.SettingsInputsIdentifier}{NameGenerator.NameSeparator}{SettingInputName}" },
                    { dataflow, settingOutput, $"{block2ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.SettingsOutputsIdentifier}{NameGenerator.NameSeparator}{SettingOutputName}" },
                    { dataflow, systemInput, $"{block2ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.SystemInputsIdentifier}{NameGenerator.NameSeparator}{SystemInputName}" },
                    { dataflow, systemOutput, $"{block2ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.SystemOutputsIdentifier}{NameGenerator.NameSeparator}{SystemOutputName}" },
                    { dataflow, processDataInput, $"{block2ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.ProcessDataInputsIdentifier}{NameGenerator.NameSeparator}{ProcessDataInputName}" },
                    { dataflow, processDataOutput, $"{block2ExpectedUniqueName}{NameGenerator.NameSeparator}{NameGenerator.ProcessDataOutputsIdentifier}{NameGenerator.NameSeparator}{ProcessDataOutputName}" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_CreatesCorrectUniqueNames))]
        public void CreatesCorrectUniqueNames(IDataflow dataflow, IIdProvider idProvider, string expectedUniqueName)
        {
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);

            uniqueNames.Should()
                .ContainKey(idProvider.Id)
                .WhoseValue
                    .Should().Be(expectedUniqueName);
        }

        public static TheoryData<IDataflow, IIdProvider, string> ArgumentsFor_CreatesCorrectLinkNames
        {
            get
            {
                const string RootContainerName = "Root";
                const string RootContainerId = "a3e6bd5a-0466-488e-aae3-a4445d0f59f7";
                const string BlockName = "Block1";
                const string BlockId = "ebf164e4-fed9-4ad0-8362-a0ac4c47c029";
                const string ProcessDataInputName = "ProcessDataInput";
                const string ProcessDataInputId = "a6219c9f-b1d6-47d8-9904-be3696ca75a6";
                const string ProcessDataOutputName = "ProcessDataOutput";
                const string ProcessDataOutputId = "fdb91ddf-c98c-49ae-9884-4e84d6cadcd7";
                const string InternalLinkId = "56710a37-a633-4e66-b98b-b6809de00d36";
                const string ExternalIncomingLinkId = "99245acc-8bc8-4b58-b48b-b18143f03c72";
                const string ExternalOutgoingLinkId = "554af8e7-ac74-4375-84f6-92b844a2a284";

                var processDataInput = Substitute.For<IConnector>();
                processDataInput.Design.Name.Returns(ProcessDataInputName);
                processDataInput.Id.Returns(new Guid(ProcessDataInputId));

                var processDataOutput = Substitute.For<IConnector>();
                processDataOutput.Design.Name.Returns(ProcessDataOutputName);
                processDataOutput.Id.Returns(new Guid(ProcessDataOutputId));

                var block = Substitute.For<IFunctionBlock>();
                block.Name.Returns(BlockName);
                block.Id.Returns(new Guid(BlockId));
                block.ProcessDataInputs.Returns([processDataInput]);
                block.ProcessDataOutputs.Returns([processDataOutput]);

                var internalLink = Substitute.For<IInternalLink>();
                internalLink.Id.Returns(new Guid(InternalLinkId));
                internalLink.Source.Returns(processDataOutput);
                internalLink.Destination.Returns(processDataInput);

                var externalIncomingLink = Substitute.For<IExternalIncomingLink>();
                externalIncomingLink.Id.Returns(new Guid(ExternalIncomingLinkId));
                externalIncomingLink.Destination.Returns(processDataInput);

                var externalOutgoingLink = Substitute.For<IExternalOutgoingLink>();
                externalOutgoingLink.Id.Returns(new Guid(ExternalOutgoingLinkId));
                externalOutgoingLink.Source.Returns(processDataOutput);

                var dataflow = Substitute.For<IDataflow>();
                dataflow.RootContainer.Name.Returns(RootContainerName);
                dataflow.RootContainer.Id.Returns(new Guid(RootContainerId));
                dataflow.RootContainer.FunctionBlocks.Returns([block]);
                dataflow.Links.Returns([internalLink, externalIncomingLink, externalOutgoingLink]);

                var blockExpectedName = $"{RootContainerName}{NameGenerator.NameSeparator}{BlockName}";
                var processDataInputExpectedName = $"{blockExpectedName}{NameGenerator.NameSeparator}{NameGenerator.ProcessDataInputsIdentifier}{NameGenerator.NameSeparator}{ProcessDataInputName}";
                var processDataOutputExpectedName = $"{blockExpectedName}{NameGenerator.NameSeparator}{NameGenerator.ProcessDataOutputsIdentifier}{NameGenerator.NameSeparator}{ProcessDataOutputName}";
                var internalLinkExpectedName = $"{processDataOutputExpectedName}{NameGenerator.LinkSourceTargetSeparator}{processDataInputExpectedName}";
                var externalIncomingExpectedName = $"{NameGenerator.ExternalIdentifier}{NameGenerator.LinkSourceTargetSeparator}{processDataInputExpectedName}";
                var externalOutgoingExpectedName = $"{processDataOutputExpectedName}{NameGenerator.LinkSourceTargetSeparator}{NameGenerator.ExternalIdentifier}";

                return new()
                {
                    { dataflow, internalLink, internalLinkExpectedName },
                    { dataflow, externalIncomingLink, externalIncomingExpectedName },
                    { dataflow, externalOutgoingLink, externalOutgoingExpectedName },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_CreatesCorrectLinkNames))]
        public void CreatesCorrectLinkNames(IDataflow dataflow, IIdProvider idProvider, string expectedUniqueName)
        {
            var uniqueNames = NameGenerator.CreateUniqueNames(dataflow);

            uniqueNames.Should()
                .ContainKey(idProvider.Id)
                .WhoseValue
                    .Should().Be(expectedUniqueName);
        }
    }
}
