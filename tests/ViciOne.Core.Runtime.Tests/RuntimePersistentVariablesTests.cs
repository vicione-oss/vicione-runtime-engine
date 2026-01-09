using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using Xunit;

public class RuntimePersistentVariablesTests
{
    public class Add
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Initialisierung der Sammlung vereinfachen", Justification = "Es soll explizit die Add-Methode getestet werden.")]
        public void AddsVariable()
        {
            var variable = Substitute.For<IRuntimePersistentVariable<object>>();
            using var variables = new RuntimePersistentVariables();

            variables.Add(variable);

            variables.Should().Contain(variable);
        }
    }

    public class Dispose
    {
        [Fact]
        public void ClearsVariableReferences()
        {
            var variable = Substitute.For<IRuntimePersistentVariable<object>>();
            using var variables = new RuntimePersistentVariables { variable };

            variables.Dispose();

            variables.Should().BeEmpty();
        }

        [Fact]
        public void CanCallMultipleTimes()
        {
            var variable = Substitute.For<IRuntimePersistentVariable<object>>();
            using var variables = new RuntimePersistentVariables { variable };
            variables.Dispose();

            Action action = variables.Dispose;

            action.Should().NotThrow();
        }
    }

    public class GetEnumerator
    {
        [Fact]
        public void GetsEnumerator()
        {
            var variable = Substitute.For<IRuntimePersistentVariable<object>>();
            using var variables = new RuntimePersistentVariables { variable };
            var explicitEnumerator = ((IEnumerable)variables).GetEnumerator();

            var implicitEnumerator = variables.GetEnumerator();

            explicitEnumerator.MoveNext();
            implicitEnumerator.MoveNext();
            implicitEnumerator.Current.Should().BeSameAs(explicitEnumerator.Current);
        }
    }

    public class VariableChanged
    {
        [Fact]
        public void GetsRaisedWhenVariableGetsChanged()
        {
            var variable = Substitute.For<IRuntimePersistentVariable<object>>();
            using var variables = new RuntimePersistentVariables { variable };
            var variableChangedHandler = Substitute.For<Action<IRuntimePersistentVariable>>();
            variables.VariableChanged += variableChangedHandler;

            variable.VariableChanged += Raise.Event<Action<IRuntimePersistentVariable>>(variable);

            variableChangedHandler
                .Received()
                .Invoke(variable);
        }
    }

    public class ResetChangedFlags
    {
        [Fact]
        public void ResetsChangedFlags()
        {
            var variable = Substitute.For<IRuntimePersistentVariable<object>>();
            using var variables = new RuntimePersistentVariables { variable };

            variables.ResetChangedFlags();

            variable
                .Received()
                .GetAndResetChanged();
        }
    }
}
