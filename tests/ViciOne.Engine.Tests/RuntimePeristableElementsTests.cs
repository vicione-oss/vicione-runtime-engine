using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.Core.Contracts.Runtime;

public abstract class RuntimePeristableElementsTests
{
    private protected static IRuntimeFunctionBlock MockBlockWithEmptyElements<TElements, TElement>(int elementsCount, Expression<Func<IRuntimeFunctionBlock, TElements>> elementsProperty)
        where TElements : class, IEnumerable<TElement>
        where TElement : class
    {
        var block = Substitute.For<IRuntimeFunctionBlock>();
        var elements = Substitute.For<TElements>();
        var list = new List<TElement>();
        for (var i = 0; i < elementsCount; i++)
            list.Add(Substitute.For<TElement>());
        elements.GetEnumerator().Returns((_) => list.GetEnumerator());
        SetReturnValue(block, elementsProperty, elements);
        return block;

        static void SetReturnValue<TInstance, TValue>(TInstance instance, Expression<Func<TInstance, TValue>> property, TValue returnValue)
        {
            var expr = (MemberExpression)property.Body;
            var propertyInfo = (PropertyInfo)expr.Member;
            propertyInfo.GetValue(instance).Returns(returnValue);
        }
    }

    private protected static void InvokeElementChanged<TElementProvider, TElement>(Dictionary<IRuntimeFunctionBlock, TElementProvider> settingsToChange)
        where TElementProvider : IChangingElementsProvider<TElement>, IEnumerable<TElement>
    {
        foreach (var pair in settingsToChange)
            foreach (var setting in pair.Value)
                InvokeElementChanged(pair.Value, setting);
    }

    private protected static void InvokeElementChanged<TElement>(IChangingElementsProvider<TElement> provider, TElement element)
        => provider.ElementChanged += Raise.Event<Action<TElement>>(element);
}
