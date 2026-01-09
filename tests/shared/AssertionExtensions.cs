using AwesomeAssertions;
using AwesomeAssertions.Collections;
using System.Collections.Generic;

internal static class AssertionExtensions
{
    /// <summary>
    /// Checks that both lists contain the same instances.
    /// </summary>
    internal static AndConstraint<GenericCollectionAssertions<T>> ContainAllButNotMore<T>(this GenericCollectionAssertions<T> assertions, IEnumerable<T> expectedList, string because = "", params object[] becauseArgs)
    {
        assertions.CurrentAssertionChain
            .ForCondition(assertions.Subject.SequenceEqualIgnoreOrder(expectedList))
            .BecauseOf(because, becauseArgs)
            .FailWith("The lists contain different elements");

        return new AndConstraint<GenericCollectionAssertions<T>>(assertions);
    }
}
