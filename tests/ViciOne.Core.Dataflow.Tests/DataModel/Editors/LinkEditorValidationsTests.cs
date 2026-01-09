using AwesomeAssertions;
using System;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class LinkEditorValidationsTests
{
    public class ValidateDestinationValueType
    {
        [Fact]
        public void DoesNotThrowIfDestinationTypeIsSameAsValueType()
        {
            Action action = LinkEditorValidations.ValidateDestinationValueType<int, int>;

            action.Should().NotThrow();
        }

        public static TheoryData<Type, Type> ArgumentsFor_DoesNotThrowIfDestinationTypeIsArrayOfValueType => new()
        {
            { typeof(double), typeof(double[]) },
            { typeof(string[]), typeof(string[][]) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_DoesNotThrowIfDestinationTypeIsArrayOfValueType))]
        public void DoesNotThrowIfDestinationTypeIsArrayOfValueType(Type valueType, Type destinationType)
        {
            Action action = () => LinkEditorValidations.ValidateDestinationValueType(valueType, destinationType);

            action.Should().NotThrow();
        }

        public static TheoryData<Type, Type> ArgumentsFor_ThrowsInAllOtherCases => new()
        {
            { typeof(double), typeof(int) },
            { typeof(int), typeof(long[]) },
            { typeof(bool[]), typeof(bool) },
            { typeof(char[]), typeof(string[]) },
        };

        [Theory]
        [MemberData(nameof(ArgumentsFor_ThrowsInAllOtherCases))]
        public void ThrowsInAllOtherCases(Type valueType, Type destinationType)
        {
            Action action = () => LinkEditorValidations.ValidateDestinationValueType(valueType, destinationType);

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*must*same*array*");
        }
    }
}
