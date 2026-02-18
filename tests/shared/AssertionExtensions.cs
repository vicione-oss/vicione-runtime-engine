using AwesomeAssertions;
using AwesomeAssertions.Collections;
using System.Collections.Generic;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
internal static class AssertionExtensions
{
    extension<T>(GenericCollectionAssertions<T> assertions)
    {
        /// <summary>
        /// Checks that both lists contain the same instances.
        /// </summary>
        internal AndConstraint<GenericCollectionAssertions<T>> ContainAllButNotMore(IEnumerable<T> expectedList, string because = "", params object[] becauseArgs)
        {
            assertions.CurrentAssertionChain
                .ForCondition(assertions.Subject.SequenceEqualIgnoreOrder(expectedList))
                .BecauseOf(because, becauseArgs)
                .FailWith("The lists contain different elements");

            return new AndConstraint<GenericCollectionAssertions<T>>(assertions);
        }
    }
}
