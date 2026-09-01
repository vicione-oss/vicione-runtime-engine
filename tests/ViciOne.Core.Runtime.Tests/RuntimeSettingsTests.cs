using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using Xunit;

public class RuntimeSettingsTests
{
    public class Add
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "The Add method is explicitly the one to be tested.")]
        public void AddsVariable()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings();

            settings.Add(setting);

            settings.Should().Contain(setting);
        }
    }

    public class Changed
    {
        [Fact]
        public void GetsRaisedWhenSettingWasChangedAndProcessWasCalled()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };
            using var monitoredSettings = settings.Monitor();

            setting.SettingChanged += Raise.Event<Action<IRuntimeSetting>>(setting);
            settings.Process(Substitute.For<IFunctionBlockCycleInfo>());

            monitoredSettings.Should().Raise(nameof(RuntimeSettings.Changed));
        }

        [Fact]
        public void DoesNotGetRaisedWhenSettingWasChangedButProcessWasNotCalled()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };
            using var monitoredSettings = settings.Monitor();

            setting.SettingChanged += Raise.Event<Action<IRuntimeSetting>>(setting);

            monitoredSettings.Should().NotRaise(nameof(RuntimeSettings.Changed));
        }

        [Fact]
        public void DoesNotGetRaisedWhenProcessWasCalledButSettingWasNotChanged()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };
            using var monitoredSettings = settings.Monitor();

            settings.Process(Substitute.For<IFunctionBlockCycleInfo>());

            monitoredSettings.Should().NotRaise(nameof(RuntimeSettings.Changed));
        }
    }

    public class Dispose
    {
        [Fact]
        public void ClearsVariableReferences()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };

            settings.Dispose();

            settings.Should().BeEmpty();
        }

        [Fact]
        public void CanCallMultipleTimes()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };
            settings.Dispose();

            Action action = settings.Dispose;

            action.Should().NotThrow();
        }
    }

    public class GetEnumerator
    {
        [Fact]
        public void GetsEnumerator()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };
            var explicitEnumerator = ((IEnumerable)settings).GetEnumerator();

            var implicitEnumerator = settings.GetEnumerator();

            explicitEnumerator.MoveNext();
            implicitEnumerator.MoveNext();
            implicitEnumerator.Current.Should().BeSameAs(explicitEnumerator.Current);
        }
    }

    public class SettingChanged
    {
        [Fact]
        public void GetsRaisedWhenSettingGetsChanged()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };
            var settingChangedHandler = Substitute.For<Action<IRuntimeSetting>>();
            settings.SettingChanged += settingChangedHandler;

            setting.SettingChanged += Raise.Event<Action<IRuntimeSetting>>(setting);

            settingChangedHandler
                .Received()
                .Invoke(setting);
        }
    }

    public class Process
    {
        [Fact]
        public void CanProcess()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };
            var settingChangedHandler = Substitute.For<Action<IRuntimeSetting>>();
            settings.SettingChanged += settingChangedHandler;
            var changedRaised = false;
            settings.Changed += () => changedRaised = true;
            var cycleInfo = Substitute.For<IFunctionBlockCycleInfo>();
            setting.SettingChanged += Raise.Event<Action<IRuntimeSetting>>(setting);

            settings.Process(cycleInfo);

            settingChangedHandler
                .Received()
                .Invoke(setting);
            setting
                .Received()
                .Process(cycleInfo);
            changedRaised.Should().BeTrue();
        }
    }

    public class ResetChangedFlags
    {
        [Fact]
        public void ResetsChangedFlags()
        {
            var setting = Substitute.For<IRuntimeSetting<object>>();
            using var settings = new RuntimeSettings { setting };

            settings.ResetChangedFlags();

            setting
                .Received()
                .GetAndResetChanged();
        }
    }
}
