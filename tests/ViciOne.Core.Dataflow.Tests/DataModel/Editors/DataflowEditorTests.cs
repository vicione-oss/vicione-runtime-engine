using AwesomeAssertions;
using System;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class DataflowEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesDataflowIfNotSupplied()
        {
            using var editor = new DataflowEditor();

            editor.Dataflow.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfDataflowIsCreated()
        {
            using var editor = new DataflowEditor();

            editor.Dataflow.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void CreatesRootContainerIfDataflowIsCreated()
        {
            using var editor = new DataflowEditor();

            editor.Dataflow.RootContainer.Should().NotBeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreatesRootContainerEditor(bool supplyDataflow)
        {
            DataflowEditor CreateEditor() => supplyDataflow
                ? new DataflowEditor(new Dataflow() { RootContainer = new Container() })
                : new DataflowEditor();

            using var editor = CreateEditor();

            editor.RootContainerEditor.Should().NotBeNull();
        }

        [Fact]
        public void StoresSuppliedDataflow()
        {
            var dataflow = new Dataflow() { RootContainer = new Container() };

            using var editor = new DataflowEditor(dataflow);

            editor.Dataflow.Should().BeSameAs(dataflow);
        }
    }

    public class AddExternalIncomingLink
    {
        [Fact]
        public void AddsLink()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = new ExternalIncomingLinkEditor<object>(CreateAndAddInput<object>(editor.Dataflow));

            editor.AddExternalIncomingLink(linkToAdd.Link);

            editor.Dataflow.Links.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeSameAs(linkToAdd.Link);
        }

        [Fact]
        public void DoesNothingIfLinkAlreadyAdded()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = new ExternalIncomingLinkEditor<object>(CreateAndAddInput<object>(editor.Dataflow));
            editor.AddExternalIncomingLink(linkToAdd.Link);

            editor.AddExternalIncomingLink(linkToAdd.Link);

            editor.Dataflow.Links.Should().ContainSingle();
        }

        [Fact]
        public void ThrowsIfDestinationNotExisting()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = new ExternalIncomingLinkEditor<object>(new ConnectorInput<object>());

            Action action = () => editor.AddExternalIncomingLink(linkToAdd.Link);

            action.Should().Throw<InvalidOperationException>()
               .WithMessage("*destination*not*existing*");
        }

        private static ConnectorInput<T> CreateAndAddInput<T>(Dataflow dataflow)
        {
            var block = new FunctionBlock();
            var connector = new ConnectorInput<T>();
            block.ProcessDataInputs.Add(connector);
            dataflow.RootContainer.FunctionBlocks.Add(block);
            return connector;
        }
    }

    public class AddExternalOutgoingLink
    {
        [Fact]
        public void AddsLinkFromOutput()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = new ExternalOutgoingLinkEditor<object>(CreateAndAddOutput<object>(editor.Dataflow));

            editor.AddExternalOutgoingLink(linkToAdd.Link);

            editor.Dataflow.Links.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeSameAs(linkToAdd.Link);
        }

        [Fact]
        public void AddsLinkFromInput()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = new ExternalOutgoingLinkEditor<object>(CreateAndAddInput<object>(editor.Dataflow));

            editor.AddExternalOutgoingLink(linkToAdd.Link);

            editor.Dataflow.Links.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeSameAs(linkToAdd.Link);
        }

        [Fact]
        public void DoesNothingIfLinkAlreadyAdded()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = new ExternalOutgoingLinkEditor<object>(CreateAndAddOutput<object>(editor.Dataflow));
            editor.AddExternalOutgoingLink(linkToAdd.Link);

            editor.AddExternalOutgoingLink(linkToAdd.Link);

            editor.Dataflow.Links.Should().ContainSingle();
        }

        [Fact]
        public void ThrowsIfSourceNotExisting()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = new ExternalOutgoingLinkEditor<object>(new ConnectorOutput<object>());

            Action action = () => editor.AddExternalOutgoingLink(linkToAdd.Link);

            action.Should().Throw<InvalidOperationException>()
               .WithMessage("*source*not*existing*");
        }

        private static ConnectorOutput<T> CreateAndAddOutput<T>(Dataflow dataflow)
        {
            var block = new FunctionBlock();
            var connector = new ConnectorOutput<T>();
            block.ProcessDataOutputs.Add(connector);
            dataflow.RootContainer.FunctionBlocks.Add(block);
            return connector;
        }

        private static ConnectorInput<T> CreateAndAddInput<T>(Dataflow dataflow)
        {
            var block = new FunctionBlock();
            var connector = new ConnectorInput<T>();
            block.ProcessDataInputs.Add(connector);
            dataflow.RootContainer.FunctionBlocks.Add(block);
            return connector;
        }
    }

    public class AddFunctionBlockDesign
    {
        [Fact]
        public void AddsFunctionBlockDesign()
        {
            using var editor = new DataflowEditor();
            var designToAdd = CreateDesign();

            editor.AddFunctionBlockDesign(designToAdd);

            editor.Dataflow.FunctionBlockDesigns.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeSameAs(designToAdd);
        }

        [Fact]
        public void DoesNothingIfFunctionBlockDesignAlreadyAdded()
        {
            using var editor = new DataflowEditor();
            var designToAdd = CreateDesign();
            editor.AddFunctionBlockDesign(designToAdd);

            editor.AddFunctionBlockDesign(designToAdd);

            editor.Dataflow.FunctionBlockDesigns.Should().ContainSingle();
        }

        [Fact]
        public void ThrowsIfEquivalentFunctionBlockDesignWasAlreadyAdded()
        {
            using var editor = new DataflowEditor();
            var equivalentdesign = CreateDesign();
            editor.AddFunctionBlockDesign(equivalentdesign);
            var designToAdd = CreateDesign();

            Action action = () => editor.AddFunctionBlockDesign(designToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*existing*");
        }

        private static FunctionBlockDesign CreateDesign()
            => new()
            {
                Namespace = "One",
                Name = "Two",
            };
    }

    public class AddInternalLink
    {
        [Fact]
        public void AddsLink()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = CreateLink(editor.Dataflow);

            editor.AddInternalLink(linkToAdd);

            editor.Dataflow.Links.Should()
                .ContainSingle()
                    .Which.Should()
                        .BeSameAs(linkToAdd);
        }

        [Fact]
        public void DoesNothingIfLinkAlreadyAdded()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = CreateLink(editor.Dataflow);
            editor.AddInternalLink(linkToAdd);

            editor.AddInternalLink(linkToAdd);

            editor.Dataflow.Links.Should().ContainSingle();
        }

        [Fact]
        public void ThrowsIfSourceNotExisting()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = CreateLink(editor.Dataflow, addSource: false);

            Action action = () => editor.AddInternalLink(linkToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*source*not*existing*");
        }

        [Fact]
        public void ThrowsIfDestinationNotExisting()
        {
            using var editor = new DataflowEditor();
            var linkToAdd = CreateLink(editor.Dataflow, addDestination: false);

            Action action = () => editor.AddInternalLink(linkToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*destination*not*existing*");
        }

        [Fact]
        public void ThrowsIfEquivalentLinkWasAlreadyAdded()
        {
            using var editor = new DataflowEditor();
            var equivalentLink = CreateLink(editor.Dataflow);
            editor.AddInternalLink(equivalentLink);
            var source = equivalentLink.Source;
            var destination = equivalentLink.Destination;
            var linkToAdd = CreateLink(source, destination);

            Action action = () => editor.AddInternalLink(linkToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*existing*");
        }

        private static InternalLink<object, object, object> CreateLink(Dataflow dataflow, bool addSource = true, bool addDestination = true)
        {
            var sourceBlock = new FunctionBlock();
            var destinationBlock = new FunctionBlock();
            var source = new ConnectorOutput<object>();
            var destination = new ConnectorInput<object>();
            sourceBlock.ProcessDataOutputs.Add(source);
            destinationBlock.ProcessDataInputs.Add(destination);
            if (addSource)
                dataflow.RootContainer.FunctionBlocks.Add(sourceBlock);
            if (addDestination)
                dataflow.RootContainer.FunctionBlocks.Add(destinationBlock);
            return CreateLink(source, destination);
        }

        private static InternalLink<object, object, object> CreateLink(ConnectorOutput<object> source, ConnectorInput<object> destination)
        {
            var link = new InternalLink<object, object, object>
            {
                Source = source,
                Destination = destination
            };
            return link;
        }
    }

    public class Dispose
    {
        [Fact]
        public void CanDispose()
        {
            using var editor = new DataflowEditor();

            Action action = editor.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            using var editor = new DataflowEditor();
            editor.Dispose();

            Action action = editor.Dispose;

            action.Should().NotThrow();
        }
    }

    public class RemoveFunctionBlockDesign
    {
        [Fact]
        public void RemovesFunctionBlockDesign()
        {
            (var editor, var blockDesign) = CreateDataflowWithFunctionBlockDesign();

            editor.RemoveFunctionBlockDesign(blockDesign);

            editor.Dataflow.FunctionBlockDesigns.Should().NotContain(blockDesign);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ThrowsIfFunctionBlockDesignIsStillUsed(bool usedInNestedContainer)
        {
            (var editor, var blockDesign) = CreateDataflowWithFunctionBlockDesign();
            var block = new FunctionBlock() { Design = blockDesign };
            if (usedInNestedContainer)
            {
                var containerWithBlock = new Container();
                containerWithBlock.FunctionBlocks.Add(block);
                editor.RootContainerEditor.Container.Containers.Add(containerWithBlock);
            }
            else
                editor.RootContainerEditor.Container.FunctionBlocks.Add(block);

            Action action = () => editor.RemoveFunctionBlockDesign(blockDesign);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*is*use*not*remove*");
        }

        [Fact]
        public void ReturnsTrueIfFunctionBlockDesignRemoved()
        {
            (var editor, var blockDesign) = CreateDataflowWithFunctionBlockDesign();

            var returned = editor.RemoveFunctionBlockDesign(blockDesign);

            returned.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfFunctionBlockDesignNotRemoved()
        {
            (var editor, var blockDesign) = CreateDataflowWithFunctionBlockDesign();
            editor.RemoveFunctionBlockDesign(blockDesign);

            var returned = editor.RemoveFunctionBlockDesign(blockDesign);

            returned.Should().BeFalse();
        }

        private static (DataflowEditor Dataflow, FunctionBlockDesign BlockDesign) CreateDataflowWithFunctionBlockDesign()
        {
            var dataflowEditor = new DataflowEditor();
            var blockDesign = new FunctionBlockDesign();
            dataflowEditor.AddFunctionBlockDesign(blockDesign);
            return (dataflowEditor, blockDesign);
        }
    }

    public class RemoveLink
    {
        [Fact]
        public void RemovesLink()
        {
            using var editor = new DataflowEditor();
            var linkToRemove = new InternalLink<object, object, object>();
            editor.Dataflow.Links.Add(linkToRemove);

            editor.RemoveLink(linkToRemove);

            editor.Dataflow.Links.Should().NotContain(linkToRemove);
        }

        [Fact]
        public void ReturnsTrueIfLinkRemoved()
        {
            using var editor = new DataflowEditor();
            var linkToRemove = new InternalLink<object, object, object>();
            editor.Dataflow.Links.Add(linkToRemove);

            var returned = editor.RemoveLink(linkToRemove);

            returned.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfLinkNotRemoved()
        {
            using var editor = new DataflowEditor();
            var linkToRemove = new InternalLink<object, object, object>();

            var returned = editor.RemoveLink(linkToRemove);

            returned.Should().BeFalse();
        }
    }
}
