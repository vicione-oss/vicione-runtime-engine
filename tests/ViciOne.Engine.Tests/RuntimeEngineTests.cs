using DummyBlocks_EngineTests;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RealBlocks_EngineTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Engine;
using Xunit;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable correctly", Justification = "The class that tests the Dispose method is meant to be named Dispose")]
public class RuntimeEngineTests : IDisposable
{
    // only reached if a test fails, so it can be generous for slow and busy machines
    private static readonly TimeSpan s_waitTimeout = TimeSpan.FromSeconds(10);

    private TestLoggerFactory LoggerFactory { get; } = new();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not relevant for the tests")]
    void IDisposable.Dispose()
    {
        LoggerFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    public class ConnectorValueTransferInterceptor : RuntimeEngineTests
    {
        [Fact]
        public void AcceptsNewValue()
        {
            using var engine = CreateEmptyEngine();
            var interceptor = Substitute.For<IConnectorValueTransferInterceptor>();

            engine.ConnectorValueTransferInterceptor = interceptor;

            engine.ConnectorValueTransferInterceptor.Should().BeSameAs(interceptor);
        }

        [Fact]
        public void NeverReturnsNull()
        {
            using var engine = CreateEmptyEngine();

            engine.ConnectorValueTransferInterceptor.Should().NotBeNull();
        }
    }

    public class CycleFinished : RuntimeEngineTests
    {
        [Fact]
        public void GetsPublishedWhenCycleFinished()
        {
            using var engine = CreateEmptyEngine();
            var cycleFinishedRaised = false;
            engine.CycleFinished += (_, __) => cycleFinishedRaised = true;
            engine.Start();

            engine.ProcessCycle();

            cycleFinishedRaised.Should().BeTrue();
        }

        [Fact]
        public void TransmitsCorrectEngineInstance()
        {
            using var engine = CreateEmptyEngine();
            RuntimeEngine? transmittedEngine = null;
            engine.CycleFinished += (sender, _) => transmittedEngine = sender as RuntimeEngine;
            engine.Start();

            engine.ProcessCycle();

            transmittedEngine.Should().Be(engine);
        }

        [Fact]
        public void TransmitsCorrectCycleNumber()
        {
            using var engine = CreateEmptyEngine();
            var cycleNoCounter = 0ul;
            var lastCycleNo = 0ul;
            engine.CycleFinished += (_, e) =>
            {
                cycleNoCounter++;
                lastCycleNo = e.CycleNo;
            };
            engine.Start();

            engine.ProcessCycle();

            lastCycleNo.Should().Be(cycleNoCounter - 1);
        }
    }

    public class CycleStarted : RuntimeEngineTests
    {
        [Fact]
        public void GetsPublishedWhenCycleStarted()
        {
            using var engine = CreateEmptyEngine();
            var cycleStartedRaised = false;
            engine.CycleStarted += (_, __) => cycleStartedRaised = true;
            engine.Start();

            engine.ProcessCycle();

            cycleStartedRaised.Should().BeTrue();
        }

        [Fact]
        public void TransmitsCorrectEngineInstance()
        {
            using var engine = CreateEmptyEngine();
            RuntimeEngine? transmittedEngine = null;
            engine.CycleStarted += (sender, _) => transmittedEngine = sender as RuntimeEngine;
            engine.Start();

            engine.ProcessCycle();

            transmittedEngine.Should().Be(engine);
        }

        [Fact]
        public void TransmitsCorrectCycleNumber()
        {
            using var engine = CreateEmptyEngine();
            var cycleNoCounter = 0ul;
            var lastCycleNo = 0ul;
            engine.CycleStarted += (_, e) =>
            {
                cycleNoCounter++;
                lastCycleNo = e.CycleNo;
            };
            engine.Start();

            engine.ProcessCycle();

            lastCycleNo.Should().Be(cycleNoCounter - 1);
        }
    }

    public class Dispose : RuntimeEngineTests
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Gets called by Should().NotThrow().")]
        public void CanDispose()
        {
            var engine = CreateEmptyEngine();

            Action action = engine.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanCallDisposeMultipleTimes()
        {
            var engine = CreateEmptyEngine();
            engine.Dispose();

            Action action = engine.Dispose;

            action.Should().NotThrow();
        }

        [Fact]
        public void StopsEngineWhenDisposed()
        {
            var engine = CreateEmptyEngine();
            engine.Start();

            engine.Dispose();

            engine.State.Should().Be(EngineState.Stopped);
        }
    }

    public class PoolingValidator : RuntimeEngineTests
    {
        [Fact]
        public void AcceptsNewValue()
        {
            using var engine = CreateEmptyEngine();
            var validator = Substitute.For<IPoolingValidator>();

            engine.PoolingValidator = validator;

            engine.PoolingValidator.Should().Be(validator);
        }

        [Fact]
        public void NeverReturnsNull()
        {
            using var engine = CreateEmptyEngine();

            engine.PoolingValidator.Should().NotBeNull();
        }
    }

    public class ProcessCycle : RuntimeEngineTests
    {
        [Fact]
        public void ChangesStateToRunning()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();

            engine.ProcessCycle();

            engine.State.Should().Be(EngineState.Running);
        }

        [Fact]
        public void ThrowsIfStateIsUnstarted()
        {
            using var engine = CreateEmptyEngine();

            Action action = () => engine.ProcessCycle();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage($"*invalid*{engine.State}*");
        }

        [Fact]
        public void ThrowsIfStateIsStopped()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();
            engine.Stop();

            Action action = () => engine.ProcessCycle();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage($"*invalid*{engine.State}*");
        }

        [Fact]
        public void ThrowsIfStateIsInitError()
        {
            using var engine = CreateEmptyEngine();
            AddBlock(typeof(RealBlock), null, engine);
            engine.Start();

            Action action = () => engine.ProcessCycle();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage($"*invalid*{engine.State}*");
        }

        [Fact]
        public void ThrowsIfPreviousCycleWasNotCompletedYet()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var block = AddBlock(runtimeBlockType, null, engine);
            using var waitForForCycleStartedWaitHandle = new ManualResetEvent(false);
            using var cycleWaitHandle = new ManualResetEvent(false);
            engine.CycleStarted += Engine_CycleStarted;
            RealBlock.ProcessCalled += RealBlock_ProcessCalled;
            engine.Start();
            Task.Run(engine.ProcessCycle);
            waitForForCycleStartedWaitHandle.WaitOne(s_waitTimeout).Should().BeTrue();

            Action action = () => engine.ProcessCycle();

            action.Should().Throw<InvalidOperationException>()
               .WithMessage($"*invalid*previous*cycle*finish*");

            cycleWaitHandle.Set();
            engine.CycleStarted -= Engine_CycleStarted;
            RealBlock.ProcessCalled -= RealBlock_ProcessCalled;

            void Engine_CycleStarted(object? sender, ICycleInfo e)
                => waitForForCycleStartedWaitHandle.Set();
            void RealBlock_ProcessCalled(object? sender, IFunctionBlockCycleInfo e)
            {
                if (((RealBlock)sender!).FunctionBlock == block)
                    cycleWaitHandle.WaitOne(s_waitTimeout);
            }
        }

        [Fact]
        public void CanProcessWithoutSettingsPersistence()
        {
            using var engine = CreateEmptyEngine([], [], withSettingsPersistence: false);
            engine.Start();

            Func<ICycleInfo> action = engine.ProcessCycle;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanProcessWithoutPersistentVariablesPersistence()
        {
            using var engine = CreateEmptyEngine([], [], withVariablesPersistence: false);
            engine.Start();

            Func<ICycleInfo> action = engine.ProcessCycle;

            action.Should().NotThrow();
        }

        [Fact]
        public void CanProcessWithoutCommunication()
        {
            using var engine = CreateEmptyEngine([], [], withCommunication: false);
            engine.Start();

            Func<ICycleInfo> action = engine.ProcessCycle;

            action.Should().NotThrow();
        }

        [Fact]
        public void UsesCloneHandlerForLinksIfUseReferenceTypeCloneHandlerForLinksIsTrue()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var block = AddBlock(typeof(RealBlock), null, engine);
            var sourceConnector = block.SettingsOutputs.First();
            var destinationConnector = block.SettingsInputs.First();
            var internalLink = Substitute.For<IInternalLink, IInternalLink<int, int, int>>();
            internalLink.Id.Returns(Guid.NewGuid());
            internalLink.Source.Returns(sourceConnector);
            ((ILinkWithSource<int>)internalLink).Source.Returns(sourceConnector);
            internalLink.Destination.Returns(destinationConnector);
            internalLink.TypeConversion.Returns((ITypeConversion?)null);
            engine.Dataflow.Links.Returns([internalLink]);
            var expectedValue = 42;
            int? receivedValue = null;
            engine.ReferenceTypeCloneHandler = new IntReturningCloneHandler(expectedValue);
            engine.UseReferenceTypeCloneHandlerForLinks = true;
            engine.Start();
            RealBlock.ProcessCalled += RealBlock_ProcessCalled;
            engine.ProcessCycle();

            engine.ProcessCycle();
            RealBlock.ProcessCalled -= RealBlock_ProcessCalled;

            receivedValue.Should().Be(expectedValue);

            void RealBlock_ProcessCalled(object? sender, IFunctionBlockCycleInfo e)
            {
                var runtimeBlock = (RealBlock)sender!;
                if (runtimeBlock.FunctionBlock == block)
                {
                    if (e.IsFirstCycle)
                        ((RuntimeSetting<int>)runtimeBlock.Settings.First()).SetValueIfNotEqual(666);
                    else
                        receivedValue = (int)runtimeBlock.SettingsInputs.First().Value!;
                }
            }
        }

        [Fact]
        public void DoesNotUseCloneHandlerForLinksIfUseReferenceTypeCloneHandlerForLinksIsFalse()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var block = AddBlock(typeof(RealBlock), null, engine);
            var sourceConnector = block.SettingsOutputs.First();
            var destinationConnector = block.SettingsInputs.First();
            var internalLink = Substitute.For<IInternalLink, IInternalLink<int, int, int>>();
            internalLink.Id.Returns(Guid.NewGuid());
            internalLink.Source.Returns(sourceConnector);
            ((ILinkWithSource<int>)internalLink).Source.Returns(sourceConnector);
            internalLink.Destination.Returns(destinationConnector);
            internalLink.TypeConversion.Returns((ITypeConversion?)null);
            engine.Dataflow.Links.Returns([internalLink]);
            var notExpectedValue = 42;
            int? receivedValue = null;
            engine.ReferenceTypeCloneHandler = new IntReturningCloneHandler(notExpectedValue);
            engine.UseReferenceTypeCloneHandlerForLinks = false;
            engine.Start();
            RealBlock.ProcessCalled += RealBlock_ProcessCalled;
            engine.ProcessCycle();

            engine.ProcessCycle();
            RealBlock.ProcessCalled -= RealBlock_ProcessCalled;

            receivedValue.Should().NotBeNull().And.NotBe(notExpectedValue);

            void RealBlock_ProcessCalled(object? sender, IFunctionBlockCycleInfo e)
            {
                var runtimeBlock = (RealBlock)sender!;
                if (runtimeBlock.FunctionBlock == block)
                {
                    if (e.IsFirstCycle)
                        ((RuntimeSetting<int>)runtimeBlock.Settings.First()).SetValueIfNotEqual(666);
                    else
                        receivedValue = (int)runtimeBlock.SettingsInputs.First().Value!;
                }
            }
        }

        [Fact]
        public void ReturnsCorrectCycleNumber()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();
            engine.ProcessCycle();

            var cycleNo = engine.ProcessCycle().CycleNo;

            cycleNo.Should().Be(1);
        }

        [Fact]
        public void ReturnsCorrectTime()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();
            engine.ProcessCycle();
            var timeBefore = DateTime.UtcNow;

            var time = engine.ProcessCycle().Time;

            time.Should().BeOnOrAfter(timeBefore)
                .And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public void PersistsChangedSettings()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var block = AddBlock(runtimeBlockType, null, engine);
            var setting = block.Settings.First();
            var settingsPersistence = engine.SettingsPersistence!;
            RealBlock.ProcessCalled += RealBlock_ProcessCalled;
            engine.Start();

            engine.ProcessCycle();
            RealBlock.ProcessCalled -= RealBlock_ProcessCalled;

            settingsPersistence
                .Received()
                .Persist(Arg.Is<IEnumerable<IPersistenceEntry>>(entries => entries
                    .Any(entry => entry.Id == setting.Id)));

            void RealBlock_ProcessCalled(object? sender, IFunctionBlockCycleInfo e)
            {
                var runtimeBlock = (RealBlock)sender!;
                if (runtimeBlock.FunctionBlock == block)
                    runtimeBlock.Settings.First().MarkAsChanged();
            }
        }

        [Fact]
        public void PersistsChangedPersistentVariables()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var block = AddBlock(runtimeBlockType, null, engine);
            var persistentVariable = block.PersistentVariables.First();
            var persistentVariablesPersistence = engine.VariablesPersistence!;
            RealBlock.ProcessCalled += RealBlock_ProcessCalled;
            engine.Start();

            engine.ProcessCycle();
            RealBlock.ProcessCalled -= RealBlock_ProcessCalled;

            persistentVariablesPersistence
                .Received()
                .Persist(Arg.Is<IEnumerable<IPersistenceEntry>>(entries => entries
                    .Any(entry => entry.Id == persistentVariable.Id)));

            void RealBlock_ProcessCalled(object? sender, IFunctionBlockCycleInfo e)
            {
                var runtimeBlock = (RealBlock)sender!;
                if (runtimeBlock.FunctionBlock == block)
                    runtimeBlock.PersistentVariables.First().MarkAsChanged();
            }
        }

        private sealed class IntReturningCloneHandler(int value) : IReferenceTypeCloneHandler
        {
            public int Value { get; } = value;

            public T? CloneIfNecessary<T>(T? value) => value is int ? (T)(object)Value : value;
        }
    }

    public class Start : RuntimeEngineTests
    {
        [Fact]
        public void ChangesStateToStartingIfSuccessfullyInitialized()
        {
            using var engine = CreateEmptyEngine();

            engine.Start();

            engine.State.Should().Be(EngineState.Starting);
        }

        [Fact]
        public void SelectsBlocksInAllNestedContainers()
        {
            using var engine = CreateEmptyEngine();
            var level1container = Substitute.For<IContainer>();
            var level2container = Substitute.For<IContainer>();
            level1container.Containers.Returns([level2container]);
            engine.Dataflow.RootContainer.Containers.Returns([level1container]);
            AddBlock(typeof(DummyBlock_ThrowsInConstructor), null, engine);
            AddBlock(typeof(DummyBlock_ThrowsInSetup), null, engine, level1container);
            AddBlock(typeof(DummyBlock_ThrowsInPhase1), null, engine, level2container);

            engine.Start();

            engine.State.Should().Be(EngineState.InitializationError);
            var errorMessages = LoggerFactory.GetLoggers().First().GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(3);
            errorMessages.Should().ContainMatch($"*Design*{nameof(DummyBlock_ThrowsInConstructor)}*");
            errorMessages.Should().ContainMatch($"*Design*{nameof(DummyBlock_ThrowsInSetup)}*");
            errorMessages.Should().ContainMatch($"*Design*{nameof(DummyBlock_ThrowsInPhase1)}*");
        }

        [Fact]
        public void CanHandleMissingBlockAssembly()
        {
            using var engine = CreateEmptyEngine();
            AddBlock(typeof(RealBlock), null, engine);

            engine.Start();

            engine.State.Should().Be(EngineState.InitializationError);
            var errorMessages = LoggerFactory.GetLoggers().First().GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should().ContainEquivalentOf("design").And.Contain(nameof(RealBlock));
        }

        [Theory]
        [InlineData(typeof(DummyBlock_ThrowsInConstructor), "*creat*instance*")]
        [InlineData(typeof(DummyBlock_ThrowsInSetup), "*creat*instance*")]
        [InlineData(typeof(DummyBlock_ThrowsInPhase1), "*phase 1*")]
        [InlineData(typeof(DummyBlock_ThrowsInPhase2), "*phase 2*")]
        public void CanHandleBlockInitializationErrors(Type runtimeBlockType, string expectedMessage)
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var block = AddBlock(runtimeBlockType, null, engine);
            block.Name.Returns("DummyBlock");

            engine.Start();

            engine.State.Should().Be(EngineState.InitializationError);
            var errorMessages = LoggerFactory.GetLoggers().First().GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should().Contain(runtimeBlockType.Name)
                .And.MatchEquivalentOf(expectedMessage);
        }

        [Fact]
        public void CanHandleMissingDriverAssembly()
        {
            using var engine = CreateEmptyEngine();
            AddBlock(typeof(RealBlock), typeof(RealDriver_EngineTests), engine);

            engine.Start();

            engine.State.Should().Be(EngineState.InitializationError);
            var errorMessages = LoggerFactory.GetLoggers().First().GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should().ContainEquivalentOf("driver").And.Contain(nameof(RealDriver_EngineTests));
        }

        [Theory]
        [InlineData(typeof(DummyDriver_EngineTests_ThrowsInConstructor))]
        [InlineData(typeof(DummyDriver_EngineTests_ThrowsInSetup))]
        [InlineData(typeof(DummyDriver_EngineTests_ThrowsInPhase1))]
        [InlineData(typeof(DummyDriver_EngineTests_ThrowsInPhase2))]
        public void CanHandleDriverInitializationErrors(Type runtimeDriverType)
        {
            using var engine = CreateEmptyEngine([Assembly.GetExecutingAssembly()], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            AddBlock(runtimeBlockType, runtimeDriverType, engine);

            engine.Start();

            engine.State.Should().Be(EngineState.InitializationError);
            var errorMessages = LoggerFactory.GetLoggers().First().GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should().Contain(runtimeDriverType.Name);
        }

        [Fact]
        public void CanStartWithoutSettingsPersistence()
        {
            using var engine = CreateEmptyEngine([], [], withSettingsPersistence: false);

            engine.Start();

            engine.State.Should().Be(EngineState.Starting);
        }

        [Fact]
        public void CanStartWithoutPersistentVariablesPersistence()
        {
            using var engine = CreateEmptyEngine([], [], withVariablesPersistence: false);

            engine.Start();

            engine.State.Should().Be(EngineState.Starting);
        }

        [Fact]
        public void CanStartWithoutCommunication()
        {
            using var engine = CreateEmptyEngine([], [], withCommunication: false);

            engine.Start();

            engine.State.Should().Be(EngineState.Starting);
        }

        [Fact]
        public void PublishesInitialValuesOfExternalOutgoingLinks()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var block = AddBlock(typeof(RealBlock), null, engine);
            var sourceConnector = block.SystemOutputs.First();
            var externalOutgoingLink = Substitute.For<IExternalOutgoingLink, IExternalOutgoingLink<bool>>();
            externalOutgoingLink.Id.Returns(Guid.NewGuid());
            externalOutgoingLink.Source.Returns(sourceConnector);
            ((ILinkWithSource<bool>)externalOutgoingLink).Source.Returns(sourceConnector);
            externalOutgoingLink.TypeConversion.Returns((ITypeConversion?)null);
            engine.Dataflow.Links.Returns([externalOutgoingLink]);
            var communicationManager = engine.CommunicationManager!;
            var expectedChangedExternalLinkIds = new[] { externalOutgoingLink.Id };

            engine.Start();

            communicationManager
              .Received()
              .HandleChangedOutgoingLinks(
                Arg.Is<IEnumerable<ILinkChangedEntry>>(
                    entries => CompareIdAndValue(entries.Single(), externalOutgoingLink.Id, sourceConnector.Value)));
        }

        private static bool CompareIdAndValue(ILinkChangedEntry entry, Guid expectedId, object? expectedValue)
              => entry.Id == expectedId && Equals(entry.Value, expectedValue);

        [Fact]
        public void ThrowsIfStateIsNotUnstarted()
        {
            using var engine = CreateEmptyEngine();
            var block = AddBlock(typeof(RealBlock), null, engine);
            engine.Start();
            var stateAfterFirstStart = engine.State;

            Action action = engine.Start;

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage($"*{EngineState.Unstarted}*{stateAfterFirstStart}*");
        }
    }

    public class StateChanged : RuntimeEngineTests
    {
        [Fact]
        public void GetsPublishedWhenStateChanged()
        {
            using var engine = CreateEmptyEngine();
            var expectedStates = new List<EngineState>()
            {
                EngineState.Initializing,
                EngineState.Initialized,
                EngineState.Starting,
                EngineState.Running,
                EngineState.Stopping,
                EngineState.Stopped,
            };
            var publishedStates = new List<EngineState>();
            engine.StateChanged += (_, __) => publishedStates.Add(engine.State);

            engine.Start();
            engine.ProcessCycle();
            engine.Stop();

            publishedStates.Should().BeEquivalentTo(expectedStates);
        }
    }

    public class Stop : RuntimeEngineTests
    {
        [Fact]
        public void SetsStateToStopped()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();

            engine.Stop();

            engine.State.Should().Be(EngineState.Stopped);
        }

        [Fact]
        public void DoesNothingIfStateIsUnstarted()
        {
            using var engine = CreateEmptyEngine();
            var stateChangedCalled = false;
            engine.StateChanged += Engine_StateChanged;

            engine.Stop();

            engine.State.Should().Be(EngineState.Unstarted);
            stateChangedCalled.Should().BeFalse();

            void Engine_StateChanged(object? sender, EventArgs e)
                => stateChangedCalled = true;
        }

        [Fact]
        public void DoesNothingIfStateIsStopping()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var block = AddBlock(runtimeBlockType, null, engine);
            using var waitForForStoppingWaitHandle = new ManualResetEvent(false);
            using var disposeWaitHandle = new ManualResetEvent(false);
            RealBlock.DisposeCalled += RealBlock_DisposeCalled;
            var stateChangedCalled = false;
            engine.StateChanged += Engine_StateChanged;
            engine.Start();
            Task.Run(engine.Stop);
            waitForForStoppingWaitHandle.WaitOne(s_waitTimeout).Should().BeTrue();
            stateChangedCalled = false;

            engine.Stop();

            engine.State.Should().Be(EngineState.Stopping);
            stateChangedCalled.Should().BeFalse();

            disposeWaitHandle.Set();
            engine.StateChanged -= Engine_StateChanged;
            RealBlock.DisposeCalled -= RealBlock_DisposeCalled;

            void RealBlock_DisposeCalled(object? sender, EventArgs e)
            {
                if (((RealBlock)sender!).FunctionBlock == block)
                    disposeWaitHandle.WaitOne(s_waitTimeout);
            }
            void Engine_StateChanged(object? sender, EventArgs e)
            {
                stateChangedCalled = true;
                if (engine.State == EngineState.Stopping)
                    waitForForStoppingWaitHandle.Set();
            }
        }

        [Fact]
        public void DoesNothingIfStateIsStopped()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();
            engine.Stop();
            var stateChangedCalled = false;
            engine.StateChanged += Engine_StateChanged;

            engine.Stop();

            engine.State.Should().Be(EngineState.Stopped);
            stateChangedCalled.Should().BeFalse();

            void Engine_StateChanged(object? sender, EventArgs e)
                => stateChangedCalled = true;
        }

        [Fact]
        public void CanStopAfterInitError()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(DummyBlock_ThrowsInSetup);
            AddBlock(runtimeBlockType, null, engine);
            engine.Start();

            var stateBeforeStop = engine.State;
            engine.Stop();

            stateBeforeStop.Should().Be(EngineState.InitializationError);
            engine.State.Should().Be(EngineState.Stopped);
        }

        [Fact]
        public void DisposesBlocksIfRunning()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var block = AddBlock(runtimeBlockType, null, engine);
            var blockDisposed = false;
            RealBlock.DisposeCalled += RealBlock_DisposeCalled;
            engine.Start();

            engine.Stop();
            RealBlock.DisposeCalled -= RealBlock_DisposeCalled;

            blockDisposed.Should().BeTrue();

            void RealBlock_DisposeCalled(object? sender, EventArgs e)
            {
                var runtimeBlock = (RealBlock)sender!;
                if (runtimeBlock.FunctionBlock == block)
                    blockDisposed = true;
            }
        }

        [Fact]
        public void DisposesBlocksIfInitError()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType1 = typeof(RealBlock);
            var runtimeBlockType2 = typeof(RealBlock_ThrowsInInPhase1);
            var block1 = AddBlock(runtimeBlockType1, null, engine);
            var block2 = AddBlock(runtimeBlockType2, null, engine);
            var block1Disposed = false;
            var block2Disposed = false;
            RealBlock.DisposeCalled += RealBlock_DisposeCalled;
            engine.Start();

            engine.Stop();
            RealBlock.DisposeCalled -= RealBlock_DisposeCalled;

            block1Disposed.Should().BeTrue();
            block2Disposed.Should().BeTrue();

            void RealBlock_DisposeCalled(object? sender, EventArgs e)
            {
                var runtimeBlock = (RealBlock)sender!;
                if (runtimeBlock.FunctionBlock == block1)
                    block1Disposed = true;
                else if (runtimeBlock.FunctionBlock == block2)
                    block2Disposed = true;
            }
        }

        [Fact]
        public void DisposesDriversIfRunning()
        {
            using var engine = CreateEmptyEngine([Assembly.GetExecutingAssembly()], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var runtimeDriverType = typeof(RealDriver_EngineTests);
            var block = AddBlock(runtimeBlockType, runtimeDriverType, engine);
            var driverDisposed = false;
            RealDriver_EngineTests.DisposeCalled += RealDriver_DisposeCalled;
            engine.Start();

            engine.Stop();
            RealDriver_EngineTests.DisposeCalled -= RealDriver_DisposeCalled;

            driverDisposed.Should().BeTrue();

            void RealDriver_DisposeCalled(object? sender, EventArgs e)
                => driverDisposed = true;
        }

        [Fact]
        public void DisposesDriversIfInitError()
        {
            using var engine = CreateEmptyEngine([Assembly.GetExecutingAssembly()], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var runtimeDriverType1 = typeof(RealDriver_EngineTests);
            var runtimeDriverType2 = typeof(RealDriver_EngineTests_ThrowsInPhase1);
            var block1 = AddBlock(runtimeBlockType, runtimeDriverType1, engine);
            var block2 = AddBlock(runtimeBlockType, runtimeDriverType2, engine);
            var driver1Disposed = false;
            var driver2Disposed = false;
            RealDriver_EngineTests.DisposeCalled += RealDriver_DisposeCalled;
            engine.Start();

            engine.Stop();
            RealDriver_EngineTests.DisposeCalled -= RealDriver_DisposeCalled;

            driver1Disposed.Should().BeTrue();
            driver2Disposed.Should().BeTrue();

            void RealDriver_DisposeCalled(object? sender, EventArgs e)
            {
                if (sender is RealDriver_EngineTests_ThrowsInPhase1)
                    driver2Disposed = true;
                else
                    driver1Disposed = true;
            }
        }

        [Fact]
        public void CanHandleBlockDisposeErorrs()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock_ThrowsInDispose);
            var block = AddBlock(runtimeBlockType, null, engine);
            var guid = Guid.NewGuid();
            block.Id.Returns(guid);
            engine.Start();
            var logger = LoggerFactory.GetLoggers().First();
            logger.ClearLoggedMessages();

            engine.Stop();

            engine.State.Should().Be(EngineState.Stopped);
            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should().Contain(guid.ToString());
        }

        [Fact]
        public void CanHandleDriverDisposeErrors()
        {
            using var engine = CreateEmptyEngine([Assembly.GetExecutingAssembly()], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var runtimeDriverType = typeof(DummyDriver_EngineTests_ThrowsInDispose);
            AddBlock(runtimeBlockType, runtimeDriverType, engine);
            engine.Start();
            var logger = LoggerFactory.GetLoggers().First();
            logger.ClearLoggedMessages();

            engine.Stop();

            engine.State.Should().Be(EngineState.Stopped);
            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            errorMessages.Should().HaveCount(1);
            errorMessages.First().Should().Contain(runtimeDriverType.Name);
        }

        [Fact]
        public void WaitsUntilInitializeIsFinished()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var block = AddBlock(runtimeBlockType, null, engine);
            using var waitForForInitializedStartedWaitHandle = new ManualResetEvent(false);
            using var initializeWaitHandle = new ManualResetEvent(false);
            var stateChangedToInitializedTime = DateTime.MinValue;
            var stateChangedToStoppingTime = DateTime.MinValue;
            engine.StateChanged += Engine_StateChanged;
            RealBlock.InitializeIICalled += RealBlock_InitializeIICalled;
            Task.Run(engine.Start);
            waitForForInitializedStartedWaitHandle.WaitOne(s_waitTimeout).Should().BeTrue();

            engine.Stop();

            stateChangedToStoppingTime.Should().BeAfter(stateChangedToInitializedTime);

            engine.StateChanged -= Engine_StateChanged;
            RealBlock.InitializeIICalled -= RealBlock_InitializeIICalled;

            void RealBlock_InitializeIICalled(object? sender, EventArgs e)
            {
                if (((RealBlock)sender!).FunctionBlock == block)
                    initializeWaitHandle.WaitOne(100);
            }
            void Engine_StateChanged(object? sender, EventArgs e)
            {
                if (engine.State == EngineState.Initializing)
                    waitForForInitializedStartedWaitHandle.Set();
                else if (engine.State == EngineState.Initialized)
                    stateChangedToInitializedTime = DateTime.UtcNow;
                else if (engine.State == EngineState.Stopping)
                    stateChangedToStoppingTime = DateTime.UtcNow;
            }
        }

        [Fact]
        public void WaitsUntilBlocksAreProcessed()
        {
            using var engine = CreateEmptyEngine([], [Assembly.GetExecutingAssembly()]);
            var runtimeBlockType = typeof(RealBlock);
            var block = AddBlock(runtimeBlockType, null, engine);
            using var waitForForCycleStartedWaitHandle = new ManualResetEvent(false);
            using var cycleWaitHandle = new ManualResetEvent(false);
            var blockDisposedTime = DateTime.MinValue;
            var blockProcessedTime = DateTime.MinValue;
            var stateChangedToStoppingTime = DateTime.MinValue;
            engine.CycleStarted += Engine_CycleStarted;
            engine.StateChanged += Engine_StateChanged;
            RealBlock.ProcessCalled += RealBlock_ProcessCalled;
            RealBlock.DisposeCalled += RealBlock_DisposeCalled;
            engine.Start();
            Task.Run(engine.ProcessCycle);
            waitForForCycleStartedWaitHandle.WaitOne(s_waitTimeout).Should().BeTrue();

            engine.Stop();

            // CycleFinished is raised after the guard is released, so it may come after the dispose
            blockProcessedTime.Should()
                .BeOnOrAfter(stateChangedToStoppingTime).And
                .BeOnOrBefore(blockDisposedTime);

            engine.CycleStarted -= Engine_CycleStarted;
            engine.StateChanged -= Engine_StateChanged;
            RealBlock.ProcessCalled -= RealBlock_ProcessCalled;
            RealBlock.DisposeCalled -= RealBlock_DisposeCalled;

            void Engine_CycleStarted(object? sender, ICycleInfo e)
                => waitForForCycleStartedWaitHandle.Set();
            void RealBlock_ProcessCalled(object? sender, IFunctionBlockCycleInfo e)
            {
                if (((RealBlock)sender!).FunctionBlock != block)
                    return;
                cycleWaitHandle.WaitOne(s_waitTimeout);
                blockProcessedTime = DateTime.UtcNow;
            }
            void RealBlock_DisposeCalled(object? sender, EventArgs e)
            {
                if (((RealBlock)sender!).FunctionBlock == block)
                    blockDisposedTime = DateTime.UtcNow;
            }
            void Engine_StateChanged(object? sender, EventArgs e)
            {
                if (engine.State == EngineState.Stopping)
                {
                    stateChangedToStoppingTime = DateTime.UtcNow;
                    // the cycle may only finish after the stop has begun
                    cycleWaitHandle.Set();
                }
            }
        }

        [Fact]
        public void PersiststSettings()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();

            engine.Stop();

            engine.SettingsPersistence!.ReceivedWithAnyArgs().Persist(Arg.Any<IEnumerable<IPersistenceEntry>>());
        }

        [Fact]
        public void PersistsPersistentVariables()
        {
            using var engine = CreateEmptyEngine();
            engine.Start();

            engine.Stop();

            engine.VariablesPersistence!.ReceivedWithAnyArgs().Persist(Arg.Any<IEnumerable<IPersistenceEntry>>());
        }
    }

    private RuntimeEngine CreateEmptyEngine()
        => CreateEmptyEngine([], []);

    private RuntimeEngine CreateEmptyEngine(IEnumerable<Assembly> driverAssemblies, IEnumerable<Assembly> functionBlockAssemblies,
        bool withSettingsPersistence = true, bool withVariablesPersistence = true, bool withCommunication = true)
    {
        var dataflow = Substitute.For<IDataflow>();
        var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
        var settingsPersistence = withSettingsPersistence ? Substitute.For<IPersistence>() : null;
        var variablesPersistence = withVariablesPersistence ? Substitute.For<IPersistence>() : null;
        var communicationManager = withCommunication ? Substitute.For<ICommunicationManager>() : null;

        return new RuntimeEngine(dataflow, LoggerFactory, driverAssemblies, functionBlockAssemblies, uniqueNameProvider,
            settingsPersistence, variablesPersistence, communicationManager);
    }

    private static IFunctionBlock AddBlock(Type runtimeBlockType, Type? runtimeDriverType, RuntimeEngine engine, IContainer? parentContainer = null)
    {
        var setting = Substitute.For<ISetting<int>>();
        setting.Id.Returns(Guid.NewGuid());
        var settingInput = Substitute.For<IConnector<int>, IPoolable>();
        setting.Input.Returns(settingInput);
        setting.Input.Id.Returns(Guid.NewGuid());
        setting.Output.Id.Returns(Guid.NewGuid());
        var settingsInputs = new IConnector[] { setting.Input };
        var settingsOutputs = new IConnector[] { setting.Output };

        var persistentVariable = Substitute.For<IPersistentVariable<int>>();
        persistentVariable.Id.Returns(Guid.NewGuid());

        var enabledInput = Substitute.For<IConnector<bool>, IPoolable>();
        enabledInput.Id.Returns(Guid.NewGuid());
        enabledInput.Design.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
        ((IConnector)enabledInput).Design.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
        enabledInput.Value.Returns(true);
        ((IConnector)enabledInput).Value.Returns(true);

        var enabledOutput = Substitute.For<IConnector<bool>>();
        enabledOutput.Id.Returns(Guid.NewGuid());
        enabledOutput.Design.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
        ((IConnector)enabledOutput).Design.Name.Returns(SystemConnectorNames.FunctionBlockEnabledName);
        enabledOutput.Value.Returns(true);
        ((IConnector)enabledOutput).Value.Returns(true);

        var block = Substitute.For<IFunctionBlock>();
        block.Design.Namespace.Returns(runtimeBlockType.Namespace);
        block.Design.Name.Returns(runtimeBlockType.Name);
        block.Design.RuntimeTypeName.Returns((string?)null);
        block.SystemInputs.Returns([enabledInput]);
        block.SystemOutputs.Returns([enabledOutput]);
        block.Settings.Returns([setting]);
        block.SettingsInputs.Returns(settingsInputs);
        block.SettingsOutputs.Returns(settingsOutputs);
        block.PersistentVariables.Returns([persistentVariable]);

        if (runtimeDriverType is not null)
            block.Design.RuntimeDriverTypeName.Returns(runtimeDriverType.FullName);
        else
            block.Design.RuntimeDriverTypeName.Returns((string?)null);

        parentContainer ??= engine.Dataflow.RootContainer;
        var fbsBefore = parentContainer.FunctionBlocks.ToArray();
        parentContainer.FunctionBlocks.Returns([.. fbsBefore, block]);

        return block;
    }
}

namespace DummyBlocks_EngineTests
{
    public class DummyBlock_ThrowsInConstructor : RuntimeFunctionBlock<DummyBlock_ThrowsInConstructor>
    {
        public DummyBlock_ThrowsInConstructor() : base()
            => throw new NotImplementedException();
    }

    public class DummyBlock_ThrowsInSetup : RuntimeFunctionBlock<DummyBlock_ThrowsInSetup>, IRuntimeFunctionBlock
    {
        void IRuntimeFunctionBlock.Setup(IFunctionBlock functionBlock, IRuntimeDriver? driver, HashSet<Guid> connectedConnectors, IUniqueNameProvider uniqueNameProvider, ILogger logger)
            => throw new NotImplementedException();
    }

    public class DummyBlock_ThrowsInPhase1 : RuntimeFunctionBlock<DummyBlock_ThrowsInPhase1>
    {
        public override void InitializeI()
            => throw new NotImplementedException();
    }

    public class DummyBlock_ThrowsInPhase2 : RuntimeFunctionBlock<DummyBlock_ThrowsInPhase2>
    {
        public override void InitializeI() { }

        public override void InitializeII()
            => throw new NotImplementedException();
    }
}

namespace RealBlocks_EngineTests
{
    public class RealBlock : RuntimeFunctionBlock<RealBlock>
    {
        public static event EventHandler? DisposeCalled;
        public static event EventHandler? InitializeIICalled;
        public static event EventHandler<IFunctionBlockCycleInfo>? ProcessCalled;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DisposeCalled?.Invoke(this, EventArgs.Empty);
        }

        public override void InitializeII()
        {
            base.InitializeII();
            InitializeIICalled?.Invoke(this, EventArgs.Empty);
        }

        protected override void InitializePersistentVariables()
            => PersistentVariables.Add(CreatePersistentVariable<int>(FunctionBlock.PersistentVariables.First().Design.Name));

        protected override void InitializeSettings()
            => Settings.Add(CreateSetting<int>(FunctionBlock.Settings.First().Design.Name));

        protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
        {
            base.ProcessCycle(cycleInfo);
            ProcessCalled?.Invoke(this, cycleInfo);
        }
    }

    public class RealBlock_ThrowsInDispose : RealBlock
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            throw new NotImplementedException();
        }
    }

    public class RealBlock_ThrowsInInPhase1 : RealBlock
    {
        public override void InitializeI()
            => throw new NotImplementedException();
    }
}

public class DummyDriver_EngineTests_ThrowsInConstructor : RuntimeDriver<DummyDriver_EngineTests_ThrowsInConstructor>
{
    public DummyDriver_EngineTests_ThrowsInConstructor()
        => throw new NotImplementedException();
}

public class DummyDriver_EngineTests_ThrowsInSetup : RuntimeDriver<DummyDriver_EngineTests_ThrowsInSetup>, IRuntimeDriver
{
    void IRuntimeDriver.Setup(ILogger logger)
        => throw new NotImplementedException();
}

public class DummyDriver_EngineTests_ThrowsInPhase1 : RuntimeDriver<DummyDriver_EngineTests_ThrowsInPhase1>
{
    public override void InitializeI()
        => throw new NotImplementedException();
}

public class DummyDriver_EngineTests_ThrowsInPhase2 : RuntimeDriver<DummyDriver_EngineTests_ThrowsInPhase2>
{
    public override void InitializeII()
        => throw new NotImplementedException();
}

public class DummyDriver_EngineTests_ThrowsInDispose : RuntimeDriver<DummyDriver_EngineTests_ThrowsInDispose>
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        throw new NotImplementedException();
    }
}

public class RealDriver_EngineTests : RuntimeDriver<RealDriver_EngineTests>
{
    public static event EventHandler? DisposeCalled;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeCalled?.Invoke(this, EventArgs.Empty);
    }
}

public class RealDriver_EngineTests_ThrowsInPhase1 : RealDriver_EngineTests
{
    public override void InitializeI()
        => throw new NotImplementedException();
}
