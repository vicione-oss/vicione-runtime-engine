using AwesomeAssertions;
using System;
using System.Reflection;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using Xunit;

public class PersistentVariableDesignEditorTests
{
    public class Constructor
    {
        [Fact]
        public void CreatesVariableIfNotSupplied()
        {
            var editor = new PersistentVariableDesignEditor<object>("variable");

            editor.PersistentVariable.Should().NotBeNull();
        }

        [Fact]
        public void SetsNewIdIfVariableIsCreated()
        {
            var editor = new PersistentVariableDesignEditor<object>("variable");

            editor.PersistentVariable.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void SetsNameIfVariableIsCreated()
        {
            var nameToSet = "zweiundvierzig";

            var editor = new PersistentVariableDesignEditor<object>(nameToSet);

            editor.PersistentVariable.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void StoresSuppliedVariable()
        {
            var variable = new PersistentVariableDesign<object>();

            var editor = new PersistentVariableDesignEditor<object>(variable);

            editor.PersistentVariable.Should().BeSameAs(variable);
        }
    }

    public class SetDefaultValue
    {
        [Fact]
        public void SetsDefaultValue()
        {
            var valueToSet = 42;
            var editor = new PersistentVariableDesignEditor<int>("variable");

            editor.SetDefaultValue(valueToSet);

            editor.PersistentVariable.DefaultValue.Should().Be(valueToSet);
        }
    }

    public class SetName
    {
        [Fact]
        public void SetsName()
        {
            var editor = new PersistentVariableDesignEditor<int>("variable");
            var nameToSet = "zweiundvierzig";

            editor.SetName(nameToSet);

            editor.PersistentVariable.Name.Should().Be(nameToSet);
        }

        [Fact]
        public void TrimsName()
        {
            var editor = new PersistentVariableDesignEditor<object>("variable");
            var nameToSet = "   Zweiundvierzig      ";
            var expectedName = "Zweiundvierzig";

            editor.SetName(nameToSet);

            editor.PersistentVariable.Name.Should().Be(expectedName);
        }

        [Fact]
        public void CallsNameNotExistingValidationMethod()
        {
            var editor = new PersistentVariableDesignEditor<object>("variable");
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            ((IPersistentVariableDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(poolingFactory);
            var nameToSet = "Zweiundvierzig";

            Action action = () => editor.SetName(nameToSet);

            action.Should().Throw<Exception>()
                .WithMessage(nameToSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsIfNameIsEmptyOrWhiteSpace(string name)
        {
            var editor = new PersistentVariableDesignEditor<object>("variable");

            Action action = () => editor.SetName(name);

            action.Should().Throw<ArgumentException>()
                .WithMessage("*not*null*empty*");
        }

        [Fact]
        public void ThrowsIfNameIsNotValidNamespaceOrClassName()
        {
            var editor = new PersistentVariableDesignEditor<object>("variable");

            Action action = () => editor.SetName("Zwei und vierzig");

            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void DoesNotSetNameIfNullOrWhiteSpaceValidationFails()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new PersistentVariableDesignEditor<object>(nameBefore);

            try { editor.SetName("   "); }
            catch { }

            editor.PersistentVariable.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfNotExistingValidationFails()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new PersistentVariableDesignEditor<object>(nameBefore);
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            ((IPersistentVariableDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(poolingFactory);

            try { editor.SetName("Sechshundertsechsundsechzig"); }
            catch { }

            editor.PersistentVariable.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotSetNameIfIsValidNamespaceOrClassNameValidationFails()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new PersistentVariableDesignEditor<object>(nameBefore);

            try { editor.SetName("Zwei und vierzig"); }
            catch { }

            editor.PersistentVariable.Name.Should().Be(nameBefore);
        }

        [Fact]
        public void DoesNotCallNameNotExistingValidationMethodIfSettingSameName()
        {
            var nameBefore = "Zweiundvierzig";
            var editor = new PersistentVariableDesignEditor<object>(nameBefore);
            var poolingFactory = new PoolingFactory([Assembly.GetExecutingAssembly()]);
            ((IPersistentVariableDesignEditor)editor).FunctionBlockDesignEditor = new MockFunctionBlockDesignEditor(poolingFactory);

            Action action = () => editor.SetName(nameBefore);

            action.Should().NotThrow();
        }

        private sealed class MockFunctionBlockDesignEditor(PoolingFactory poolingFactory) : FunctionBlockDesignEditor(poolingFactory)
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Sufficient for the functionality to be tested here")]
            internal override void ValidatePersistentVariableDesignNameNotExisting(string name)
                => throw new Exception(name);
        }
    }
}
