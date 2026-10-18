using AwesomeAssertions;
using NSubstitute;
using System;
using System.ComponentModel;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeLinkTests
{
    public class Constructor
    {
        [Fact]
        public void StoresLink()
        {
            var link = CreateLink<object>();

            var runtimeLink = new MockRuntimeLink<object, object>(link, default);

            runtimeLink.Link.Should().BeSameAs(link);
        }

        [Fact]
        public void SetsValue()
        {
            var value = 42;
            var link = CreateLink<object>();

            var runtimeLink = new MockRuntimeLink<object, object>(link, value);

            runtimeLink.Value.Should().Be(value);
        }
    }

    public class GetAndResetChanged
    {
        [Fact]
        public void GetsChangedFlag()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockRuntimeLink<object, object>(link, default);
            runtimeLink.SetData(666, default, default);

            var changed = runtimeLink.GetAndResetChanged();

            changed.Should().BeTrue();
        }

        [Fact]
        public void ResetsChangedFlag()
        {
            var link = CreateLink<object>();
            var runtimeLink = new MockRuntimeLink<object, object>(link, default);
            runtimeLink.SetData(666, default, default);

            runtimeLink.GetAndResetChanged();

            runtimeLink.Changed.Should().BeFalse();
        }
    }

    public class SetData
    {
        [Fact]
        public void SetsValue()
        {
            var link = CreateLink<int>();
            var runtimeLink = new MockRuntimeLink<int, int>(link, default);
            var value = 42;

            runtimeLink.SetData(value, default, default);

            runtimeLink.Value.Should().Be(value);
        }

        [Fact]
        public void SetsValidity()
        {
            var link = CreateLink<int>();
            var runtimeLink = new MockRuntimeLink<int, int>(link, default);
            var validity = Validity.Valid;

            runtimeLink.SetData(default(int), default, validity);

            runtimeLink.Validity.Should().Be(validity);
        }

        [Fact]
        public void SetsChangedTime()
        {
            var link = CreateLink<int>();
            var runtimeLink = new MockRuntimeLink<int, int>(link, default);
            var changedTime = DateTime.Now.AddMinutes(-1);

            runtimeLink.SetData(666, changedTime, default);

            runtimeLink.ChangedTime.Should().Be(changedTime);
        }

        [Fact]
        public void SetsChangedFlag()
        {
            var link = CreateLink<int>();
            var runtimeLink = new MockRuntimeLink<int, int>(link, default);

            runtimeLink.SetData(666, default, default);

            runtimeLink.Changed.Should().BeTrue();
        }

        public static TheoryData<IRuntimeLink, object, object> ArgumentsFor_ConvertsValue
        {
            get
            {
                var link1 = Substitute.For<ILink<bool>>();
                link1.TypeConversion!.UseConvertFrom.Returns(false);
                link1.TypeConversion!.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(double)));

                var link2 = Substitute.For<ILink<int>>();
                link2.TypeConversion!.UseConvertFrom.Returns(true);
                link2.TypeConversion!.TypeConverter.Returns(TypeDescriptor.GetConverter(typeof(int)));

                var link3 = Substitute.For<ILink<object>>();
                link3.TypeConversion.Returns((ITypeConversion?)null);

                var link4 = Substitute.For<ILink<BaseType>>();
                link4.TypeConversion.Returns((ITypeConversion?)null);
                var derivedTypeInstance = new DerivedType();

                return new()
                {
                    { new MockRuntimeLink<double, bool>(link1, default), 42, true },
                    { new MockRuntimeLink<string, int>(link2, "0"), "42", 42 },
                    { new MockRuntimeLink<int, object>(link3, 0), 42, 42 },
                    { new MockRuntimeLink<DerivedType, BaseType>(link4, null), derivedTypeInstance, derivedTypeInstance },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_ConvertsValue))]
        public void ConvertsValue(IRuntimeLink link, object valueToSet, object expectedConvertedValue)
        {
            link.SetData(valueToSet, default, default);

            link.Value.Should().Be(expectedConvertedValue);
        }

        [Fact]
        public void RaisesLinkChanged()
        {
            var link = CreateLink<int>();
            var runtimeLink = new MockRuntimeLink<int, int>(link, default);
            using var monitoredLink = runtimeLink.Monitor();

            runtimeLink.SetData(666, default, default);

            monitoredLink.Should()
                .Raise(nameof(MockRuntimeLink<,>.LinkChanged))
                .WithSender(runtimeLink);
        }
    }

    private static ILink<TValue> CreateLink<TValue>()
    {
        var link = Substitute.For<ILink<TValue>>();
        link.TypeConversion.Returns((ITypeConversion?)null);
        return link;
    }

    private sealed class MockRuntimeLink<TSource, TValue>(ILink<TValue> link, TSource? value) :
        RuntimeLink<TSource, TValue>(link, value)
    { }

    public class BaseType { }
    public sealed class DerivedType : BaseType { }
}
