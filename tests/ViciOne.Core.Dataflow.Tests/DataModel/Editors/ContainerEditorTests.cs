using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class ContainerEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesContainerIfNotSupplied()
        {
            var editor = new ContainerEditor();

            editor.Container.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfContainerIsCreated()
        {
            var editor = new ContainerEditor();

            editor.Container.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsNameIfContainerIsCreated()
        {
            var editor = new ContainerEditor();

            editor.Container.Name.Should().BeSameAs(ContainerDefaults.Name);
        }

        [Fact]
        public void StoresSuppliedContainer()
        {
            var container = new Container();

            var editor = new ContainerEditor(container);

            editor.Container.Should().BeSameAs(container);
        }
    }

    public class AddConnector
    {
        private readonly Container _container;
        private readonly Connector _directChildConnector;
        private readonly Connector _notDirectChildConnector;

        public AddConnector()
        {
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            var designEditor = new FunctionBlockDesignEditor(poolingFactory);
            designEditor.SetNamespace("Namespace");
            designEditor.SetName("Name");
            var containerEditor = new ContainerEditor();
            var childContainerEditor = new ContainerEditor();
            containerEditor.AddContainer(childContainerEditor);
            var blockEditor = new FunctionBlockEditor(designEditor.Design);
            var childBlockEditor = new FunctionBlockEditor(designEditor.Design);
            containerEditor.AddFunctionBlock(blockEditor);
            childContainerEditor.AddFunctionBlock(childBlockEditor);

            _container = containerEditor.Container;
            _directChildConnector = blockEditor.FunctionBlock.SystemInputs.First();
            _notDirectChildConnector = childBlockEditor.FunctionBlock.SystemInputs.First();
        }

        [Fact]
        public void AddsConnector()
        {
            var editor = new MockContainerEditor(_container);
            var connectorToAdd = _directChildConnector;

            editor.AddConnector(connectorToAdd, _container.Inputs);

            _container.Inputs.Should().Contain(connectorToAdd);
        }

        [Fact]
        public void DoesNothingIfConnectorIsAlreadyAdded()
        {
            var editor = new MockContainerEditor(_container);
            var connectorToAdd = _directChildConnector;
            _container.Inputs.Add(connectorToAdd);
            var countBefore = _container.Inputs.Count;

            editor.AddConnector(connectorToAdd, _container.Inputs);

            _container.Inputs.Should().HaveCount(countBefore);
        }

        [Fact]
        public void ThrowsIfConnectorIsNotDirectChild()
        {
            var editor = new MockContainerEditor(_container);
            var connectorToAdd = _notDirectChildConnector;

            Action action = () => editor.AddConnector(connectorToAdd, _container.Inputs);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*no*direct*child*");
        }

        private sealed class MockContainerEditor(Container container) : ContainerEditor(container)
        {
            public new void AddConnector(Connector connector, List<Connector> listToAddTo)
                => base.AddConnector(connector, listToAddTo);
        }
    }

    public class AddContainer
    {
        [Fact]
        public void AddsContainer()
        {
            var editor = new ContainerEditor();
            var containerToAdd = new ContainerEditor();

            editor.AddContainer(containerToAdd);

            editor.Container.Containers.Should().Contain(containerToAdd.Container);
        }

        [Fact]
        public void DoesNothingIfContainerWasAlreadyAdded()
        {
            var editor = new ContainerEditor();
            var containerToAdd = new ContainerEditor();
            editor.AddContainer(containerToAdd);
            var countBefore = editor.Container.Containers.Count;

            editor.AddContainer(containerToAdd);

            editor.Container.Containers.Should().HaveCount(countBefore);
        }

        [Fact]
        public void ThrowsIfContainerWillBeAddedToItself()
        {
            var editor = new ContainerEditor();

            Action action = () => editor.AddContainer(editor);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*not*added*itself*");
        }

        [Fact]
        public void ThrowsIfContainerWasAlreadyAddedToAnotherContainer()
        {
            var editor = new ContainerEditor();
            var containerToAdd = new ContainerEditor();
            var anotherContainer = new ContainerEditor();
            anotherContainer.AddContainer(containerToAdd);

            Action action = () => editor.AddContainer(containerToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*added*");
        }

        [Fact]
        public void ThrowsIfNameAlreadyExisting()
        {
            var containerEditor1 = new ContainerEditor();
            var containerEditor2 = new ContainerEditor();
            containerEditor1.SetName("fortytwo");
            containerEditor2.SetName("fortytwo");
            var editor = new ContainerEditor();
            editor.AddContainer(containerEditor1);

            Action action = () => editor.AddContainer(containerEditor2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*container*already*existing*");
        }

        [Fact]
        public void SetsParentContainerEditor()
        {
            var editor = new ContainerEditor();
            var containerToAdd = new ContainerEditor();

            editor.AddContainer(containerToAdd);

            containerToAdd.ParentContainerEditor.Should().Be(editor);
        }
    }

    public class AddFunctionBlock
    {
        private readonly FunctionBlockDesign _functionBlockDesign;

        public AddFunctionBlock()
        {
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            var designEditor = new FunctionBlockDesignEditor(poolingFactory);
            designEditor.SetNamespace("Namespace");
            designEditor.SetName("Name");
            _functionBlockDesign = designEditor.Design;
        }

        [Fact]
        public void AddsFunctionBlock()
        {
            var editor = new ContainerEditor();
            var functionBlockToAdd = new FunctionBlockEditor(_functionBlockDesign);

            editor.AddFunctionBlock(functionBlockToAdd);

            editor.Container.FunctionBlocks.Should().Contain(functionBlockToAdd.FunctionBlock);
        }

        [Fact]
        public void DoesNothingIfFunctionBlockWasAlreadyAdded()
        {
            var editor = new ContainerEditor();
            var functionBlockToAdd = new FunctionBlockEditor(_functionBlockDesign);
            editor.AddFunctionBlock(functionBlockToAdd);
            var countBefore = editor.Container.FunctionBlocks.Count;

            editor.AddFunctionBlock(functionBlockToAdd);

            editor.Container.FunctionBlocks.Should().HaveCount(countBefore);
        }

        [Fact]
        public void ThrowsIfFunctionBlockWasAlreadyAddedToAnotherContainer()
        {
            var editor = new ContainerEditor();
            var functionBlockToAdd = new FunctionBlockEditor(_functionBlockDesign);
            var anotherContainer = new ContainerEditor();
            anotherContainer.AddFunctionBlock(functionBlockToAdd);

            Action action = () => editor.AddFunctionBlock(functionBlockToAdd);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already*added*");
        }

        [Fact]
        public void ThrowsIfNameAlreadyExisting()
        {
            var blockEditor1 = new FunctionBlockEditor(_functionBlockDesign);
            var blockEditor2 = new FunctionBlockEditor(_functionBlockDesign);
            blockEditor1.SetName("fortytwo");
            blockEditor2.SetName("fortytwo");
            var editor = new ContainerEditor();
            editor.AddFunctionBlock(blockEditor1);

            Action action = () => editor.AddFunctionBlock(blockEditor2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*block*already*existing*");
        }

        [Fact]
        public void SetsContainerEditor()
        {
            var editor = new ContainerEditor();
            var functionBlockToAdd = new FunctionBlockEditor(_functionBlockDesign);

            editor.AddFunctionBlock(functionBlockToAdd);

            functionBlockToAdd.ContainerEditor.Should().Be(editor);
        }
    }

    public class AddInputOutput
    {
        protected class MockContainerEditor : ContainerEditor
        {
            public List<Connector> PassedList { get; private set; } = [];

            protected override void AddConnector(Connector connector, List<Connector> listToAddTo)
                => PassedList = listToAddTo;
        }
    }

    public class AddInput : AddInputOutput
    {
        [Fact]
        public void PassesCorrectConnectorList()
        {
            var editor = new MockContainerEditor();

            editor.AddInput(new ConnectorInput<object>());

            editor.PassedList.Should().BeSameAs(editor.Container.Inputs);
        }
    }

    public class AddOutput : AddInputOutput
    {
        [Fact]
        public void PassesCorrectConnectorList()
        {
            var editor = new MockContainerEditor();

            editor.AddOutput(new ConnectorOutput<object>());

            editor.PassedList.Should().BeSameAs(editor.Container.Outputs);
        }
    }

    public class RemoveContainer
    {
        [Fact]
        public void RemovesContainer()
        {
            var editor = new ContainerEditor();
            var containerToRemove = new ContainerEditor();
            editor.AddContainer(containerToRemove);

            editor.RemoveContainer(containerToRemove);

            editor.Container.Containers.Should().NotContain(containerToRemove.Container);
        }

        [Fact]
        public void RemovesInputs()
        {
            var (parentEditor, childEditor) = CreateContainerWithConnectors();

            parentEditor.RemoveContainer(childEditor);

            parentEditor.Container.Inputs.Should().HaveCount(0);
        }

        [Fact]
        public void RemovesOutputs()
        {
            var (parentEditor, childEditor) = CreateContainerWithConnectors();

            parentEditor.RemoveContainer(childEditor);

            parentEditor.Container.Outputs.Should().HaveCount(0);
        }

        [Fact]
        public void ReturnsTrueIfContainerRemoved()
        {
            var editor = new ContainerEditor();
            var containerToRemove = new ContainerEditor();
            editor.AddContainer(containerToRemove);

            var returned = editor.RemoveContainer(containerToRemove);

            returned.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfContainerNotRemoved()
        {
            var editor = new ContainerEditor();
            var containerToRemove = new ContainerEditor();

            var returned = editor.RemoveContainer(containerToRemove);

            returned.Should().BeFalse();
        }

        [Fact]
        public void ResetsParentContainerEditor()
        {
            var editor = new ContainerEditor();
            var containerToRemove = new ContainerEditor();
            editor.AddContainer(containerToRemove);

            editor.RemoveContainer(containerToRemove);

            containerToRemove.ParentContainerEditor.Should().BeNull();
        }

        private static (ContainerEditor Parent, ContainerEditor Child) CreateContainerWithConnectors()
        {
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            var designEditor = new FunctionBlockDesignEditor(poolingFactory);
            designEditor.SetNamespace("Namespace");
            designEditor.SetName("Name");
            var containerEditor = new ContainerEditor();
            var childContainerEditor = new ContainerEditor();
            containerEditor.AddContainer(childContainerEditor);
            var childBlockEditor = new FunctionBlockEditor(designEditor.Design);
            childContainerEditor.AddFunctionBlock(childBlockEditor);
            var inputToAdd = childBlockEditor.FunctionBlock.SystemInputs.First();
            var outputToAdd = childBlockEditor.FunctionBlock.SystemOutputs.First();
            childContainerEditor.AddInput(inputToAdd);
            childContainerEditor.AddOutput(outputToAdd);
            containerEditor.AddInput(inputToAdd);
            containerEditor.AddOutput(outputToAdd);
            return (containerEditor, childContainerEditor);
        }
    }

    public class RemoveFunctionBlock
    {
        [Fact]
        public void RemovesFunctionBlock()
        {
            (var editor, var block) = CreateContainerWithFunctionBlock();

            editor.RemoveFunctionBlock(block);

            editor.Container.FunctionBlocks.Should().NotContain(block.FunctionBlock);
        }

        [Fact]
        public void RemovesInputs()
        {
            (var editor, var block) = CreateContainerWithConnectors();

            editor.RemoveFunctionBlock(block);

            editor.Container.Inputs.Should().HaveCount(0);
        }

        [Fact]
        public void RemovesOutputs()
        {
            (var editor, var block) = CreateContainerWithConnectors();

            editor.RemoveFunctionBlock(block);

            editor.Container.Outputs.Should().HaveCount(0);
        }

        [Fact]
        public void ReturnsTrueIfFunctionBlockRemoved()
        {
            (var editor, var block) = CreateContainerWithFunctionBlock();

            var returned = editor.RemoveFunctionBlock(block);

            returned.Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalseIfFunctionBlockNotRemoved()
        {
            (var editor, var block) = CreateContainerWithFunctionBlock();
            editor.RemoveFunctionBlock(block);

            var returned = editor.RemoveFunctionBlock(block);

            returned.Should().BeFalse();
        }

        [Fact]
        public void ResetsContainerEditor()
        {
            (var editor, var block) = CreateContainerWithFunctionBlock();

            editor.RemoveFunctionBlock(block);

            block.ContainerEditor.Should().BeNull();
        }

        private static (ContainerEditor Container, FunctionBlockEditor Block) CreateContainerWithFunctionBlock()
        {
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            var designEditor = new FunctionBlockDesignEditor(poolingFactory);
            designEditor.SetNamespace("Namespace");
            designEditor.SetName("Name");
            var containerEditor = new ContainerEditor();
            var blockEditor = new FunctionBlockEditor(designEditor.Design);
            containerEditor.AddFunctionBlock(blockEditor);
            return (containerEditor, blockEditor);
        }

        private static (ContainerEditor Container, FunctionBlockEditor Block) CreateContainerWithConnectors()
        {
            (var containerEditor, var blockEditor) = CreateContainerWithFunctionBlock();
            var inputToAdd = blockEditor.FunctionBlock.SystemInputs.First();
            var outputToAdd = blockEditor.FunctionBlock.SystemOutputs.First();
            containerEditor.AddInput(inputToAdd);
            containerEditor.AddOutput(outputToAdd);
            return (containerEditor, blockEditor);
        }
    }

    public class RemoveInput
    {
        [Fact]
        public void RemovesInput()
        {
            var container = new Container();
            container.Inputs.Add(new ConnectorInput<object>());
            var editor = new ContainerEditor(container);

            editor.RemoveInput(container.Inputs.First());

            container.Inputs.Should().HaveCount(0);
        }
    }

    public class RemoveInputs
    {
        [Fact]
        public void RemovesInputs()
        {
            var container = new Container();
            var connectors = new[]
            {
                new ConnectorInput<object>(),
                new ConnectorInput<object>(),
                new ConnectorInput<object>()
            };
            container.Inputs.AddRange(connectors);
            var editor = new ContainerEditor(container);

            editor.RemoveInputs(connectors);

            container.Inputs.Should().HaveCount(0);
        }
    }

    public class RemoveOutput
    {
        [Fact]
        public void RemovesOutput()
        {
            var container = new Container();
            container.Outputs.Add(new ConnectorOutput<object>());
            var editor = new ContainerEditor(container);

            editor.RemoveOutput(container.Outputs.First());

            container.Outputs.Should().HaveCount(0);
        }
    }

    public class RemoveOutputs
    {
        [Fact]
        public void RemovesOutputs()
        {
            var container = new Container();
            var connectors = new[]
            {
                new ConnectorOutput<object>(),
                new ConnectorOutput<object>(),
                new ConnectorOutput<object>()
            };
            container.Outputs.AddRange(connectors);
            var editor = new ContainerEditor(container);

            editor.RemoveOutputs(connectors);

            container.Outputs.Should().HaveCount(0);
        }
    }

    public class SetName
    {
        [Fact]
        public void SetsName()
        {
            var editor = new ContainerEditor();
            var nameToSet = "FortyTwo";

            editor.SetName(nameToSet);

            editor.Container.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void TrimsName()
        {
            var editor = new ContainerEditor();
            var nameToSet = "   FortyTwo      ";
            var expectedName = "FortyTwo";

            editor.SetName(nameToSet);

            editor.Container.Name.Should().Be(expectedName);
        }

        [Fact]
        public void CallsNameNotExistingValidationMethod()
        {
            var editor = new ContainerEditor
            {
                ParentContainerEditor = new MockContainerEditor()
            };
            var nameToSet = "FortyTwo";

            Action action = () => editor.SetName(nameToSet);

            action.Should().Throw<Exception>()
                .WithMessage(nameToSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNameIsEmptyOrWhiteSpace(string name)
        {
            var editor = new ContainerEditor();

            Action action = () => editor.SetName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var editor = new ContainerEditor();
            var nameBefore = "FortyTwo";
            editor.SetName(nameBefore);

            try { editor.SetName("   "); }
            catch { }

            editor.Container.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfNotExistingValidationFails()
        {
            var editor = new ContainerEditor();
            var nameBefore = "FortyTwo";
            editor.SetName(nameBefore);
            editor.ParentContainerEditor = new MockContainerEditor();

            try { editor.SetName("SixHundredSixtySix"); }
            catch { }

            editor.Container.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotCallNameNotExistingValidationMethodIfSettingSameName()
        {
            var editor = new ContainerEditor();
            var nameBefore = "FortyTwo";
            editor.SetName(nameBefore);
            editor.ParentContainerEditor = new MockContainerEditor();

            Action action = () => editor.SetName(nameBefore);

            action.Should().NotThrow();
        }

        private sealed class MockContainerEditor : ContainerEditor
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Sufficient for the functionality to be tested here")]
            protected internal override void ValidateFunctionBlockOrContainerNameNotExisting(string name)
                => throw new Exception(name);
        }
    }
}
