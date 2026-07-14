using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.Cloning;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

/// <summary>
/// Tests for <see cref="DeepCloner"/>.
/// </summary>
public class DeepClonerTests
{
    // =========================================================================
    // Functional tests
    // =========================================================================

    public class NullHandling
    {
        [Fact]
        public void DeepClone_NullReferenceType_ReturnsNull()
        {
            var result = DeepCloner.DeepClone<SimplePoco>(null);
            result.Should().BeNull();
        }

        [Fact]
        public void DeepClone_NullableStructNull_ReturnsNull()
        {
            var result = DeepCloner.DeepClone<int?>(null);
            result.Should().BeNull();
        }

        [Fact]
        public void DeepClone_NullableStructWithValue_ReturnsSameValue()
        {
            int? value = 42;
            var result = DeepCloner.DeepClone(value);
            result.Should().Be(42);
        }
    }

    public class ImmutableTypes
    {
        [Theory]
        [InlineData(0)]
        [InlineData(42)]
        [InlineData(-1)]
        public void DeepClone_Int_ReturnsSameValue(int value)
        {
            var result = DeepCloner.DeepClone(value);
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(3.14)]
        public void DeepClone_Double_ReturnsSameValue(double value)
        {
            var result = DeepCloner.DeepClone(value);
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DeepClone_Bool_ReturnsSameValue(bool value)
        {
            var result = DeepCloner.DeepClone(value);
            result.Should().Be(value);
        }

        [Fact]
        public void DeepClone_String_ReturnsSameReference()
        {
            const string S = "hello";
            var result = DeepCloner.DeepClone(S);
            // Strings are immutable — the same reference is returned.
            result.Should().BeSameAs(S);
        }

        [Fact]
        public void DeepClone_HeapAllocatedString_ReturnsSameReference()
        {
            // new string(…) produces a non-interned heap allocation. Asserting BeSameAs
            // here proves the cloner returns the original reference, not an interned copy.
            var s = new string('x', 5);
            var result = DeepCloner.DeepClone(s);
            result.Should().BeSameAs(s);
        }

        [Fact]
        public void DeepClone_Enum_ReturnsSameValue()
        {
            const DayOfWeek Value = DayOfWeek.Wednesday;
            var result = DeepCloner.DeepClone(Value);
            result.Should().Be(Value);
        }

        [Fact] // Runtime reflection singletons — Type
        public void DeepClone_Type_ReturnsSameReference()
        {
            var t = typeof(string);
            var result = DeepCloner.DeepClone(t);
            result.Should().BeSameAs(t);
        }

        [Fact] // Runtime reflection singletons — FieldInfo
        public void DeepClone_FieldInfo_ReturnsSameReference()
        {
            var fi = typeof(string).GetField("Empty")!;
            var result = DeepCloner.DeepClone(fi);
            result.Should().BeSameAs(fi);
        }

        [Fact] // Runtime reflection singletons — Assembly
        public void DeepClone_Assembly_ReturnsSameReference()
        {
            var asm = typeof(string).Assembly;
            var result = DeepCloner.DeepClone(asm);
            result.Should().BeSameAs(asm);
        }
    }

    public class SimplePocoCloningTests
    {
        [Fact]
        public void DeepClone_SimplePoco_IsNotReferenceEqualButFieldsAreEqual()
        {
            var original = new SimplePoco { X = 7, Y = "hello" };
            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.X.Should().Be(original.X);
            clone.Y.Should().Be(original.Y);
        }

        [Fact]
        public void DeepClone_NestedPoco_BothInstancesAreNew()
        {
            var inner = new SimplePoco { X = 5, Y = "inner" };
            var outer = new NestedPoco { Value = 1, Inner = inner };

            var clone = DeepCloner.DeepClone(outer)!;

            clone.Should().NotBeSameAs(outer);
            clone.Inner.Should().NotBeSameAs(inner);
            clone.Inner!.X.Should().Be(inner.X);
            clone.Inner.Y.Should().Be(inner.Y);
        }

        [Fact]
        public void DeepClone_ReadonlyField_IsCopied()
        {
            var original = new ReadonlyFieldClass(42, "readonly-value");
            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.GetId().Should().Be(original.GetId());
            clone.GetLabel().Should().Be(original.GetLabel());
        }

        [Fact]
        public void DeepClone_TypeWithoutParameterlessConstructor_ClonesSuccessfully()
        {
            var original = new NoDefaultCtorClass(99, "param");
            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Value.Should().Be(original.Value);
            clone.Name.Should().Be(original.Name);
        }

        [Fact]
        public void DeepClone_TypeWithNoInterfaceOrAttributes_ClonesSuccessfully()
        {
            var original = new PlainClass { Num = 3 };
            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Num.Should().Be(original.Num);
        }
    }

    public class ValueTypeCloningTests
    {
        [Fact]
        public void DeepClone_StructContainingImmutableField_ImmutableReferenceIsShared()
        {
            // StructWithRef is a pure value type (its only field is an immutable string),
            // so DeepClone returns it via the shallow-copy fast path. Use a non-interned
            // string so BeSameAs proves the inner reference is shared, not re-allocated.
            var name = new string('t', 4);
            var original = new StructWithRef { Name = name };
            var clone = DeepCloner.DeepClone(original);

            clone.Name.Should().BeSameAs(name);
        }

        [Fact]
        public void DeepClone_StructContainingObjectField_CloneReferenceIsNewInstance()
        {
            var inner = new SimplePoco { X = 1, Y = "y" };
            var original = new StructWithObject { Inner = inner };
            var clone = DeepCloner.DeepClone(original);

            clone.Inner.Should().NotBeSameAs(inner);
            clone.Inner!.X.Should().Be(inner.X);
        }
    }

    public class ArrayCloningTests
    {
        [Fact]
        public void DeepClone_SingleDimArray_NewArrayWithDeepClonedElements()
        {
            var inner = new SimplePoco { X = 1, Y = "a" };
            var original = new[] { inner, new SimplePoco { X = 2, Y = "b" } };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Length.Should().Be(original.Length);
            clone[0].Should().NotBeSameAs(original[0]);
            clone[0].X.Should().Be(original[0].X);
        }

        [Fact]
        public void DeepClone_MultiDimArray_NewArrayWithEqualElements()
        {
            var original = new int[2, 3] { { 1, 2, 3 }, { 4, 5, 6 } };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.GetLength(0).Should().Be(2);
            clone.GetLength(1).Should().Be(3);
            for (var i = 0; i < 2; i++)
                for (var j = 0; j < 3; j++)
                    clone[i, j].Should().Be(original[i, j]);
        }

        [Fact]
        public void DeepClone_JaggedArray_InnerArraysAreNewInstances()
        {
            var inner0 = new[] { new SimplePoco { X = 1 } };
            var inner1 = new[] { new SimplePoco { X = 2 } };
            var original = new[] { inner0, inner1 };

            var clone = DeepCloner.DeepClone(original)!;

            clone[0].Should().NotBeSameAs(inner0);
            clone[1].Should().NotBeSameAs(inner1);
            clone[0][0].Should().NotBeSameAs(inner0[0]);
            clone[0][0].X.Should().Be(inner0[0].X);
        }

        [Fact]
        public void DeepClone_StringArray_NewContainerElementsReferenceEqual()
        {
            // Use new string(…) to allocate strings that the CLR will NOT intern,
            // so BeSameAs asserts the cloner actually returns the same reference rather
            // than an accidentally-interned copy of equal content.
            var original = new[] { new string('a', 1), new string('b', 1), new string('c', 1) };
            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            for (var i = 0; i < original.Length; i++)
                clone[i].Should().BeSameAs(original[i]);
        }
    }

    public class CollectionCloningTests
    {
        [Fact]
        public void DeepClone_ListOfPocos_NewListWithNewElements()
        {
            var original = new List<SimplePoco>
            {
                new() { X = 1, Y = "a" },
                new() { X = 2, Y = "b" }
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().HaveCount(original.Count);
            for (var i = 0; i < original.Count; i++)
            {
                clone[i].Should().NotBeSameAs(original[i]);
                clone[i].X.Should().Be(original[i].X);
            }
        }

        [Fact]
        public void DeepClone_Dictionary_NewDictionaryWithClonedEntries()
        {
            var original = new Dictionary<string, SimplePoco>
            {
                ["a"] = new() { X = 10, Y = "x" },
                ["b"] = new() { X = 20, Y = "y" }
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().HaveCount(original.Count);
            clone["a"].Should().NotBeSameAs(original["a"]);
            clone["a"].X.Should().Be(original["a"].X);
            clone["b"].X.Should().Be(original["b"].X);
        }
    }

    // =========================================================================
    // Circular / shared reference tests
    // =========================================================================

    public class CircularAndSharedReferenceTests
    {
        [Fact]
        public void DeepClone_SelfReferencingObject_NoStackOverflow_CloneSelf()
        {
            var node = new Node { Value = 1 };
            node.Next = node; // self-reference

            var clone = DeepCloner.DeepClone(node)!;

            clone.Should().NotBeSameAs(node);
            clone.Next.Should().BeSameAs(clone); // clone.Self == clone
        }

        [Fact]
        public void DeepClone_TwoNodeCycle_CyclePreservedInClone()
        {
            var a = new Node { Value = 1 };
            var b = new Node { Value = 2 };
            a.Next = b;
            b.Next = a;

            var cloneA = DeepCloner.DeepClone(a)!;

            cloneA.Should().NotBeSameAs(a);
            cloneA.Next.Should().NotBeSameAs(b);
            cloneA.Next!.Next.Should().BeSameAs(cloneA); // cycle preserved
        }

        [Fact]
        public void DeepClone_SharedReference_BothFieldsPointToSameSingleClone()
        {
            var shared = new SimplePoco { X = 99 };
            var original = new TwoFieldObject { First = shared, Second = shared };

            var clone = DeepCloner.DeepClone(original)!;

            clone.First.Should().NotBeSameAs(shared);
            clone.First.Should().BeSameAs(clone.Second); // shared in clone too
        }

        [Fact]
        public void DeepClone_CollectionContainingItself_ClonesWithoutInfiniteRecursion()
        {
            var list = new List<object>();
            list.Add(list); // self-reference

            var clone = DeepCloner.DeepClone(list)!;

            clone.Should().NotBeSameAs(list);
            clone[0].Should().BeSameAs(clone); // self-reference preserved
        }
    }

    // =========================================================================
    // Delegate / event tests
    // =========================================================================

    public class DelegateAndEventTests
    {
        [Fact]
        public void DeepClone_ObjectWithActionField_DelegateIsCopiedByReference()
        {
            static void Action() { }
            var original = new WithDelegateField { Handler = Action };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Handler.Should().BeSameAs(original.Handler);
        }

        [Fact]
        public void DeepClone_ObjectWithEventField_EventHandlerListCopiedByReference()
        {
            var original = new WithEvent();
            static void Handler() { }
            original.SomeEvent += Handler;

            var clone = DeepCloner.DeepClone(original)!;

            // The backing delegate field is copied by reference (not cloned).
            clone.GetEventDelegate().Should().BeSameAs(original.GetEventDelegate());
        }
    }

    // =========================================================================
    // Caching / independence tests
    // =========================================================================

    public class CachingTests
    {
        [Fact]
        public void DeepClone_SameTypeTwice_BothResultsIndependentAndValid()
        {
            var original = new SimplePoco { X = 1, Y = "first" };

            var clone1 = DeepCloner.DeepClone(original)!;
            var clone2 = DeepCloner.DeepClone(original)!;

            clone1.Should().NotBeSameAs(clone2);
            clone1.X.Should().Be(original.X);
            clone2.X.Should().Be(original.X);
        }

        [Fact]
        public void DeepClone_MutatingOneCloneDoesNotAffectAnother()
        {
            var original = new SimplePoco { X = 10, Y = "original" };
            var clone1 = DeepCloner.DeepClone(original)!;
            var clone2 = DeepCloner.DeepClone(original)!;

            clone1.X = 999;

            clone2.X.Should().Be(10);
            original.X.Should().Be(10);
        }
    }

    // =========================================================================
    // Thread safety tests
    // =========================================================================

    public class ThreadSafetyTests
    {
        [Fact]
        public void DeepClone_SameTypeFromManyThreads_NoExceptions_AllResultsValid()
        {
            const int Iterations = 200;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var results = new System.Collections.Concurrent.ConcurrentBag<SimplePoco>();

            Parallel.For(0, Iterations, _ =>
            {
                try
                {
                    var original = new SimplePoco { X = 42, Y = "thread" };
                    var clone = DeepCloner.DeepClone(original)!;
                    results.Add(clone);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty();
            results.Should().HaveCount(Iterations);
            results.Should().AllSatisfy(r =>
            {
                r.X.Should().Be(42);
                r.Y.Should().Be("thread");
            });
        }

        [Fact]
        public void DeepClone_ManyDistinctTypesFromManyThreads_NoExceptions()
        {
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.Invoke(
                () => TryClone(() => new SimplePoco { X = 1 }, exceptions),
                () => TryClone(() => new NestedPoco { Value = 2 }, exceptions),
                () => TryClone(() => new Node { Value = 3 }, exceptions),
                () => TryClone(() => new NoDefaultCtorClass(4, "x"), exceptions),
                () => TryClone(() => new List<int> { 1, 2, 3 }, exceptions));

            exceptions.Should().BeEmpty();

            static void TryClone<T>(Func<T> factory, System.Collections.Concurrent.ConcurrentBag<Exception> bag)
            {
                try { DeepCloner.DeepClone(factory()); }
                catch (Exception ex) { bag.Add(ex); }
            }
        }
    }

    // =========================================================================
    // ALC safety test
    // =========================================================================

    public class AlcSafetyTests
    {
        /// <summary>
        /// A collectible type is added to the DeepCloner cache as a KEY, then the ALC is
        /// unloaded. ConditionalWeakTable must release the entry so the Type (and therefore
        /// the ALC) can be collected.
        /// </summary>
        [Fact]
        public void DeepClone_AfterAlcUnload_CollectibleTypeIsReleasedFromCache()
        {
            WeakReference? collectibleTypeRef = null;
            WarmCacheWithCollectibleType(ref collectibleTypeRef);

            // Give the GC multiple attempts to collect the now-unreachable type.
            for (var i = 0; i < 20; i++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                if (!collectibleTypeRef!.IsAlive)
                    break;
                Thread.Sleep(20);
            }

            collectibleTypeRef!.IsAlive.Should().BeFalse(
                "ConditionalWeakTable must not prevent the collectible Type from being collected; " +
                "if this fails the cache holds a strong reference to the key Type");
        }

        /// <summary>
        /// Isolated into a NoInlining method so the JIT does not keep local references
        /// alive on the calling frame during subsequent GC cycles.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WarmCacheWithCollectibleType(ref WeakReference? typeRef)
        {
            var alc = new AssemblyLoadContext("deep-cloner-alc-test", isCollectible: true);

            // Load ViciOne.Core.Dataflow into a collectible ALC.  This gives us a second,
            // ALC-local copy of every type in that assembly.
            var asm = alc.LoadFromAssemblyPath(typeof(DeepCloner).Assembly.Location);

            // Resolve a non-shallow-copyable type from the collectible copy of the assembly.
            // CloneContext has a Dictionary field, so it will not hit the shallow-copy
            // fast-path and will therefore be registered as a key in s_cache.
            var collectibleType = asm.GetType(
                "ViciOne.Core.Dataflow.Cloning.CloneContext", throwOnError: true)!;

            // Track the Type object (the CWT key), NOT the ALC.
            // The Type is what the cache must release; the ALC follows once the type is gone.
            typeRef = new WeakReference(collectibleType, trackResurrection: true);

            // Create an uninitialized instance of the collectible type and deep-clone it
            // through the MAIN-PROCESS DeepCloner.  This is the call that writes
            // (collectibleType → delegate) into the main process's s_cache.
            var instance = RuntimeHelpers.GetUninitializedObject(collectibleType);
            DeepCloner.DeepClone(instance); // ← s_cache is touched here

            // Unload the ALC. Local variables go out of scope when this NoInlining method returns,
            // releasing the last strong references to both instance and collectibleType.
            alc.Unload();
        }
    }

    // =========================================================================
    // Domain integration tests
    // =========================================================================

    public class DomainIntegrationTests
    {
        [Fact]
        public void ConnectorInputEditor_ClonedPoolingIsNotSameInstance()
        {
            var pooling = new DummyPooling();
            var design = new ConnectorDesignInput<int>
            {
                Name = "test",
                Id = Guid.NewGuid(),
                DefaultCrossSourcePoolingStrategy = PoolingStrategy.Aggregate,
                DefaultCrossSourceAggregatingPooling = pooling,
                DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue,
            };

            var editor = new ConnectorInputEditor<int>(design);

            editor.Connector.CrossSourceAggregatingPooling.Should()
                .NotBeSameAs(pooling);
            editor.Connector.CrossSourceAggregatingPooling.Should()
                .BeOfType<DummyPooling>();
        }

        private sealed class DummyPooling : IAggregatingPooling<int>
        {
            public string Name => "dummy";
            public string Description => "dummy";
            public int Aggregate(IEnumerable<int> values) => 0;
        }
    }

    // =========================================================================
    // Reflection-type field tests (Type/MemberInfo/Assembly as fields)
    // =========================================================================

    public class ReflectionTypeFieldTests
    {
        [Fact] // Class with Type-typed field — must not corrupt the cloned Type
        public void DeepClone_ClassWithTypeField_TypeFieldIsSharedByReference()
        {
            var t = typeof(int);
            var original = new WithTypeField { StoredType = t, Name = "test" };
            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Name.Should().Be("test");
            clone.StoredType.Should().BeSameAs(t);
        }

        [Fact] // Struct that flows through the value-type clone delegate: the non-shallow
               // Inner field makes the struct impure, forcing the delegate path so the
               // per-field pre-classification and the Type guard are genuinely exercised.
        public void DeepClone_StructWithTypeField_TypeFieldIsSharedByReference()
        {
            var t = typeof(double);
            var inner = new SimplePoco { X = 7 };
            var original = new StructWithTypeField { StoredType = t, Value = 7, Inner = inner };
            var clone = DeepCloner.DeepClone(original);

            clone.Value.Should().Be(7);
            clone.StoredType.Should().BeSameAs(t, "Type fields must be shared by reference, never deep-cloned");
            clone.Inner.Should().NotBeSameAs(inner, "the mutable reference field must be deep-cloned");
            clone.Inner!.X.Should().Be(7);
        }
    }

    // =========================================================================
    // Multi-dimensional array deep-clone tests (CopyMultiDimArray coverage)
    // =========================================================================

    public class MultiDimArrayDeepCloneTests
    {
        [Fact] // Exercises CopyMultiDimArray with reference-type elements
        public void DeepClone_MultiDimArrayOfPocos_EachElementIsNewInstance()
        {
            var original = new SimplePoco[2, 2]
            {
                { new SimplePoco { X = 1, Y = "a" }, new SimplePoco { X = 2, Y = "b" } },
                { new SimplePoco { X = 3, Y = "c" }, new SimplePoco { X = 4, Y = "d" } }
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.GetLength(0).Should().Be(2);
            clone.GetLength(1).Should().Be(2);

            for (var i = 0; i < 2; i++)
                for (var j = 0; j < 2; j++)
                {
                    clone[i, j].Should().NotBeSameAs(original[i, j], "each element must be a new instance");
                    clone[i, j].X.Should().Be(original[i, j].X);
                    clone[i, j].Y.Should().Be(original[i, j].Y);
                }
        }

        [Fact] // Non-zero lower bounds must be preserved in the clone
        public void DeepClone_MultiDimArrayWithNonZeroLowerBound_ClonesCorrectly()
        {
            // Create int[3..4, 10..11] — lower bounds [3, 10]
            var original = Array.CreateInstance(typeof(int), [2, 2], [3, 10]);
            original.SetValue(7, 3, 10);
            original.SetValue(8, 3, 11);
            original.SetValue(9, 4, 10);
            original.SetValue(10, 4, 11);

            var clone = DeepCloner.DeepClone(original)!;

            clone.GetLowerBound(0).Should().Be(3);
            clone.GetLowerBound(1).Should().Be(10);
            clone.GetValue(3, 10).Should().Be(7);
            clone.GetValue(4, 11).Should().Be(10);
        }
    }

    // =========================================================================
    // readonly field holding a mutable reference (IsInitOnly branch coverage)
    // =========================================================================

    public class ReadonlyMutableFieldTests
    {
        [Fact] // IsInitOnly + non-shallow payload → SetValue must deep-clone the inner object
        public void DeepClone_ReadonlyFieldHoldingMutableObject_InnerObjectIsNewInstance()
        {
            var inner = new SimplePoco { X = 42, Y = "inner" };
            var original = new ContainerWithReadonlyRef(inner);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.GetInner().Should().NotBeSameAs(inner,
                "readonly does not mean immutable; the inner object must be deep-cloned");
            clone.GetInner().X.Should().Be(42);
        }

        [Fact] // Mutating original's inner must not affect the clone's independent copy
        public void DeepClone_MutatingOriginalReadonlyInner_DoesNotAffectClone()
        {
            var inner = new SimplePoco { X = 1, Y = "original" };
            var original = new ContainerWithReadonlyRef(inner);

            var clone = DeepCloner.DeepClone(original)!;
            inner.X = 999;

            clone.GetInner().X.Should().Be(1,
                "clone holds an independent copy; mutating original's inner must not affect it");
        }
    }

    // =========================================================================
    // Array edge cases
    // =========================================================================

    public class ArrayEdgeCaseTests
    {
        [Fact] // Null elements inside an array must be preserved (tests null guard in CloneObject)
        public void DeepClone_ArrayContainingNullElements_NullsPreservedInClone()
        {
            var original = new SimplePoco?[] { null, new() { X = 1 }, null };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone[0].Should().BeNull();
            clone[1].Should().NotBeSameAs(original[1]);
            clone[1]!.X.Should().Be(1);
            clone[2].Should().BeNull();
        }

        [Fact] // Empty 1-D array — no elements to iterate
        public void DeepClone_EmptyArray_ReturnsNewEmptyArray()
        {
            var original = Array.Empty<SimplePoco>();

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().BeEmpty();
        }

        [Fact] // Zero-length multi-dim array
        public void DeepClone_EmptyMultiDimArray_ReturnsNewEmptyArray()
        {
            var original = new SimplePoco[0, 3];

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.GetLength(0).Should().Be(0);
            clone.GetLength(1).Should().Be(3);
        }
    }

    // =========================================================================
    // Collection edge cases
    // =========================================================================

    public class CollectionEdgeCaseTests
    {
        [Fact] // Empty list
        public void DeepClone_EmptyList_ReturnsNewEmptyList()
        {
            var original = new List<SimplePoco>();

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().BeEmpty();
        }

        [Fact] // Empty dictionary
        public void DeepClone_EmptyDictionary_ReturnsNewEmptyDictionary()
        {
            var original = new Dictionary<string, int>();

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().BeEmpty();
        }

        [Fact] // Dictionary with reference-type keys — exercises shallowKey=false branch
        public void DeepClone_DictionaryWithPocoKey_KeysAreNewInstances()
        {
            var k1 = new SimplePoco { X = 1 };
            var k2 = new SimplePoco { X = 2 };
            var original = new Dictionary<SimplePoco, string>(ReferenceEqualityComparer.Instance)
            {
                [k1] = "one",
                [k2] = "two"
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().HaveCount(2);
            // Keys must be new instances (cloned, not the originals)
            clone.Keys.Should().NotContain(k1, "key must be a new deep-cloned instance");
            clone.Keys.Should().NotContain(k2, "key must be a new deep-cloned instance");
            var clonedK1 = clone.Keys.Single(k => k.X == 1);
            clone[clonedK1].Should().Be("one");
        }

        [Fact] // Custom comparer must survive cloning so the clone behaves identically
        public void DeepClone_DictionaryWithCustomComparer_ComparerIsPreservedInClone()
        {
            var original = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Key"] = 42
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone["KEY"].Should().Be(42, "cloned dictionary must preserve the case-insensitive comparer");
        }

        [Fact] // Dictionary comparers are intentionally preserved by reference
        public void DeepClone_DictionaryWithMutableComparer_ComparerIsSharedByReference()
        {
            var comparer = new MutableModuloComparer { Modulo = 10 };
            var original = new Dictionary<int, string>(comparer)
            {
                [1] = "one"
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Comparer.Should().BeSameAs(comparer,
                "dictionary comparers define lookup semantics and are preserved by reference");

            comparer.Modulo = 5;
            ((MutableModuloComparer)clone.Comparer).Modulo.Should().Be(5);
        }

        [Fact] // Guards the enumeration-based fast path against loop rewrite regressions
        public void DeepClone_LargeDictionary_AllEntriesRoundTrip()
        {
            var original = new Dictionary<int, SimplePoco>();
            for (var i = 0; i < 1_000; i++)
                original[i] = new SimplePoco { X = i, Y = $"value-{i}" };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().HaveCount(1_000);
            clone[0].Should().NotBeSameAs(original[0]);
            clone[999].X.Should().Be(999);
            clone[999].Y.Should().Be("value-999");
        }
    }

    // =========================================================================
    // Polymorphic / interface-typed and abstract-typed fields
    // =========================================================================

    public class PolymorphicFieldTests
    {
        [Fact] // Declared type IShape, runtime type Circle — cloner dispatches on runtime type
        public void DeepClone_InterfaceTypedField_RuntimeTypeIsClonedCorrectly()
        {
            var original = new ShapeHolder { Shape = new Circle { Radius = 5 } };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Shape.Should().NotBeSameAs(original.Shape);
            clone.Shape.Should().BeOfType<Circle>("runtime type must be preserved");
            ((Circle)clone.Shape!).Radius.Should().Be(5);
        }

        [Fact] // Abstract base class field — concrete derived type must be preserved
        public void DeepClone_AbstractTypedField_ConcreteDerivedTypeIsClonedCorrectly()
        {
            var original = new Zoo { Resident = new Dog { Name = "Rex", Breed = "Labrador" } };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Resident.Should().NotBeSameAs(original.Resident);
            clone.Resident.Should().BeOfType<Dog>();
            ((Dog)clone.Resident!).Breed.Should().Be("Labrador");
        }
    }

    // =========================================================================
    // Inheritance — base-class fields must be cloned alongside derived fields
    // =========================================================================

    public class InheritanceTests
    {
        [Fact] // GetAllInstanceFields must walk the full hierarchy
        public void DeepClone_DerivedClass_BaseClassFieldsAreClonedToo()
        {
            var original = new Derived
            {
                BaseProp = new SimplePoco { X = 1 },
                DerivedProp = new SimplePoco { X = 2 }
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.BaseProp.Should().NotBeSameAs(original.BaseProp,
                "base-class field must be independently cloned");
            clone.BaseProp!.X.Should().Be(1);
            clone.DerivedProp.Should().NotBeSameAs(original.DerivedProp);
            clone.DerivedProp!.X.Should().Be(2);
        }
    }

    // =========================================================================
    // C# record types (init-only compiler-generated backing fields)
    // =========================================================================

    public class RecordCloningTests
    {
        [Fact] // Records use IsInitOnly backing fields → SetValue branch must handle them
        public void DeepClone_Record_IsNewInstanceWithEqualValues()
        {
            var original = new Point(3, 7);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.X.Should().Be(3);
            clone.Y.Should().Be(7);
        }

        [Fact] // Nested records — each level must be a new instance
        public void DeepClone_NestedRecord_AllLevelsAreNewInstances()
        {
            var original = new Line(new Point(0, 0), new Point(10, 10));

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Start.Should().NotBeSameAs(original.Start,
                "record with reference fields must be deep-cloned");
            clone.Start.X.Should().Be(0);
            clone.End.X.Should().Be(10);
        }
    }

    // =========================================================================
    // object-typed field boxing edge cases
    // =========================================================================

    public class BoxedValueTests
    {
        [Fact] // Object field holding a boxed int → runtime type is shallow-copyable
        public void DeepClone_ObjectFieldHoldingBoxedInt_ReturnsSameBoxedValue()
        {
            var original = new BoxHolder { Value = 42 };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Value.Should().Be(42);
        }

        [Fact] // Object field holding a POCO → runtime type requires deep clone
        public void DeepClone_ObjectFieldHoldingPoco_PocoIsDeepCloned()
        {
            var inner = new SimplePoco { X = 7 };
            var original = new BoxHolder { Value = inner };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Value.Should().NotBeSameAs(inner,
                "runtime type is SimplePoco which is not shallow-copyable");
            ((SimplePoco)clone.Value!).X.Should().Be(7);
        }

        [Fact] // Boxed structs can participate in object-reference cycles
        public void DeepClone_ObjectFieldHoldingBoxedStructCycle_PreservesCycle()
        {
            object boxed = new BoxedSelfReferencingStruct { Value = 7 };
            typeof(BoxedSelfReferencingStruct)
                .GetProperty(nameof(BoxedSelfReferencingStruct.Self))!
                .SetValue(boxed, boxed);
            var original = new BoxHolder { Value = boxed };

            var clone = DeepCloner.DeepClone(original)!;

            var clonedBox = clone.Value!;
            clonedBox.Should().NotBeSameAs(boxed);
            clonedBox.Should().BeOfType<BoxedSelfReferencingStruct>();
            var clonedValue = (BoxedSelfReferencingStruct)clonedBox;
            clonedValue.Value.Should().Be(7);
            clonedValue.Self.Should().BeSameAs(clonedBox);
        }
    }

    // =========================================================================
    // Deep / tall object graph — no stack overflow
    // =========================================================================

    public class DeepGraphTests
    {
        // NOTE: the current implementation is recursive (CloneObject → delegate → CloneObject).
        // At ~6 stack frames per node level, 200 nodes fits safely in a 4 MB thread stack even
        // in a test-runner environment that already has substantial stack usage.
        // If the implementation is changed to an iterative approach this limit can be raised.
        [Fact] // Moderately deep linked list must not cause a StackOverflowException
        public void DeepClone_DeepLinkedList_DoesNotOverflowStack()
        {
            const int Depth = 200;
            var head = new Node { Value = 0 };
            var current = head;
            for (var i = 1; i < Depth; i++)
            {
                current.Next = new Node { Value = i };
                current = current.Next;
            }

            var clone = DeepCloner.DeepClone(head);

            var orig = head;
            var cloned = clone;
            var count = 0;
            while (orig is not null)
            {
                cloned.Should().NotBeNull();
                cloned!.Value.Should().Be(orig.Value);
                cloned.Should().NotBeSameAs(orig);
                orig = orig.Next;
                cloned = cloned.Next;
                count++;
            }
            count.Should().Be(Depth);
        }
    }

    // =========================================================================
    // Runtime-generated proxies — shared by reference, never traversed
    // =========================================================================

    public class RuntimeProxyTests
    {
        [Fact] // Traversing a mock would walk into the mocking framework's shared state
        public void DeepClone_NSubstituteProxy_ReturnsSameReference()
        {
            var substitute = Substitute.For<IAggregatingPooling<object>>();

            var clone = DeepCloner.DeepClone(substitute);

            clone.Should().BeSameAs(substitute);
        }

        [Fact] // The pass-through must not break the substitute's call tracking
        public void DeepClone_NSubstituteProxy_CallTrackingStillWorksAfterClone()
        {
            var substitute = Substitute.For<IAggregatingPooling<object>>();

            var clone = DeepCloner.DeepClone(substitute)!;
            clone.Aggregate([]);

            substitute.Received(1).Aggregate(Arg.Any<IEnumerable<object?>>());
        }

        [Fact] // A proxy reached through a field is shared while the rest is deep-cloned
        public void DeepClone_ObjectWithProxyField_ProxyIsSharedRestIsCloned()
        {
            var substitute = Substitute.For<IAggregatingPooling<object>>();
            var holder = new ProxyHolder
            {
                Pooling = substitute,
                Poco = new SimplePoco { X = 42, Y = "zweiundvierzig" },
            };

            var clone = DeepCloner.DeepClone(holder)!;

            clone.Should().NotBeSameAs(holder);
            clone.Pooling.Should().BeSameAs(substitute);
            clone.Poco.Should().NotBeSameAs(holder.Poco);
            clone.Poco!.X.Should().Be(42);
        }

        private class ProxyHolder
        {
            public IAggregatingPooling<object>? Pooling { get; set; }
            public SimplePoco? Poco { get; set; }
        }
    }

    // =========================================================================
    // Native resource guards — shared by reference, never duplicated
    // =========================================================================

    public class CriticalFinalizerTests
    {
        [Fact] // Duplicating a SafeHandle would double-free the native handle on finalize
        public void DeepClone_SafeHandle_ReturnsSameReference()
        {
            using var handle = new Microsoft.Win32.SafeHandles.SafeFileHandle(IntPtr.Zero, ownsHandle: false);

            var clone = DeepCloner.DeepClone(handle);

            clone.Should().BeSameAs(handle);
        }

        [Fact] // A handle reached through a field is shared while the rest is deep-cloned
        public void DeepClone_ObjectWithSafeHandleField_HandleIsSharedRestIsCloned()
        {
            using var handle = new Microsoft.Win32.SafeHandles.SafeFileHandle(IntPtr.Zero, ownsHandle: false);
            var holder = new HandleHolder
            {
                Handle = handle,
                Poco = new SimplePoco { X = 7, Y = "sieben" },
            };

            var clone = DeepCloner.DeepClone(holder)!;

            clone.Should().NotBeSameAs(holder);
            clone.Handle.Should().BeSameAs(handle);
            clone.Poco.Should().NotBeSameAs(holder.Poco);
        }

        [Fact] // Boxed-as-object handles must be caught by runtime-type dispatch too
        public void DeepClone_ObjectFieldHoldingSafeHandle_HandleIsShared()
        {
            using var handle = new Microsoft.Win32.SafeHandles.SafeFileHandle(IntPtr.Zero, ownsHandle: false);
            var holder = new BoxHolder { Value = handle };

            var clone = DeepCloner.DeepClone(holder)!;

            clone.Value.Should().BeSameAs(handle);
        }

        private class HandleHolder
        {
            public Microsoft.Win32.SafeHandles.SafeFileHandle? Handle { get; set; }
            public SimplePoco? Poco { get; set; }
        }
    }

    // =========================================================================
    // Domain integration — ConnectorInputEditor with stateful poolings
    // =========================================================================

    public class StatefulPoolingIsolationTests
    {
        [Fact]
        public void TwoEditors_WithStatefulPooling_WeightDictionaryIsIndependent()
        {
            var template = new WeightedAveragePooling
            {
                DefaultWeight = 0.5,
                Weights = new Dictionary<string, double> { ["source-A"] = 2.0 }
            };
            var design = BuildDesign(template);

            var editor1 = new ConnectorInputEditor<double>(design);
            var editor2 = new ConnectorInputEditor<double>(design);

            var pooling1 = (WeightedAveragePooling)editor1.Connector.CrossSourceAggregatingPooling!;
            var pooling2 = (WeightedAveragePooling)editor2.Connector.CrossSourceAggregatingPooling!;

            pooling1.DefaultWeight.Should().Be(0.5);
            pooling1.Weights["source-A"].Should().Be(2.0);

            pooling1.DefaultWeight = 99.0;
            pooling1.Weights["source-A"] = 999.0;
            pooling1.Weights["source-B"] = 42.0;

            pooling2.DefaultWeight.Should().Be(0.5,
                "modifying editor1's clone must not change editor2's independent clone");
            pooling2.Weights["source-A"].Should().Be(2.0,
                "the cloned Weights dictionary must be independent");
            pooling2.Weights.Should().NotContainKey("source-B");

            template.DefaultWeight.Should().Be(0.5,
                "the design's original pooling must never be modified through editor clones");
            template.Weights.Should().HaveCount(1);
        }

        [Fact]
        public void MutatingDesignTemplate_AfterEditorCreated_DoesNotAffectExistingEditor()
        {
            var template = new WeightedAveragePooling { DefaultWeight = 1.0 };
            var design = BuildDesign(template);

            var editor = new ConnectorInputEditor<double>(design);

            template.DefaultWeight = 999.0;

            var pooling = (WeightedAveragePooling)editor.Connector.CrossSourceAggregatingPooling!;
            pooling.DefaultWeight.Should().Be(1.0,
                "editor received a deep clone at construction time; later template changes must not bleed through");
        }

        private static ConnectorDesignInput<double> BuildDesign(IAggregatingPooling<double> pooling)
            => new()
            {
                Name = "sensor",
                Id = Guid.NewGuid(),
                DefaultCrossSourcePoolingStrategy = PoolingStrategy.Aggregate,
                DefaultCrossSourceAggregatingPooling = pooling,
                DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue,
            };

        private sealed class WeightedAveragePooling : IAggregatingPooling<double>
        {
            public string Name => "WeightedAverage";
            public string Description => "Weighted average with configurable source weights.";
            public Dictionary<string, double> Weights { get; set; } = [];
            public double DefaultWeight { get; set; } = 1.0;
            public double Aggregate(IEnumerable<double> values) => values.Sum() * DefaultWeight;
        }
    }

    public class ResetToDefaultsTests
    {
        [Fact]
        public void ResetToDefaults_RestoresPoolingToFreshClone_NotSameInstanceAsBeforeReset()
        {
            var template = new SimpleStatefulPooling { Weight = 0.5 };
            var design = BuildDesign(template);

            var editor = new ConnectorInputEditor<double>(design);
            var poolingBefore = editor.Connector.CrossSourceAggregatingPooling;

            editor.ResetToDefaults();
            var poolingAfter = editor.Connector.CrossSourceAggregatingPooling;

            poolingAfter.Should().NotBeSameAs(poolingBefore,
                "ResetToDefaults must allocate a fresh clone, not re-use the previous instance");
            poolingAfter.Should().NotBeSameAs(template,
                "ResetToDefaults must not expose the design's template directly");
            ((SimpleStatefulPooling)poolingAfter!).Weight.Should().Be(0.5);
        }

        [Fact]
        public void TwoEditors_AfterBothReset_PoolingsAreStillIndependent()
        {
            var template = new SimpleStatefulPooling { Weight = 1.0 };
            var design = BuildDesign(template);

            var editor1 = new ConnectorInputEditor<double>(design);
            var editor2 = new ConnectorInputEditor<double>(design);

            editor1.ResetToDefaults();
            editor2.ResetToDefaults();

            editor1.Connector.CrossSourceAggregatingPooling.Should()
                .NotBeSameAs(editor2.Connector.CrossSourceAggregatingPooling,
                    "each reset must produce an independent clone");
        }

        private static ConnectorDesignInput<double> BuildDesign(IAggregatingPooling<double> pooling)
            => new()
            {
                Name = "x",
                Id = Guid.NewGuid(),
                DefaultCrossSourcePoolingStrategy = PoolingStrategy.Aggregate,
                DefaultCrossSourceAggregatingPooling = pooling,
                DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue,
            };

        private sealed class SimpleStatefulPooling : IAggregatingPooling<double>
        {
            public string Name => "Stateful";
            public string Description => "";
            public double Weight { get; set; } = 1.0;
            public double Aggregate(IEnumerable<double> values) => values.Sum() * Weight;
        }
    }

    public class NullPoolingIntegrationTests
    {
        [Fact]
        public void Editor_WithNullPoolingInDesign_DoesNotThrow()
        {
            var design = new ConnectorDesignInput<double>
            {
                Name = "x",
                Id = Guid.NewGuid(),
                DefaultCrossSourcePoolingStrategy = PoolingStrategy.Queue,
                DefaultCrossSourceAggregatingPooling = null,
                DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue,
                DefaultPerSourceAggregatingPooling = null,
            };

            Action act = () => _ = new ConnectorInputEditor<double>(design);
            act.Should().NotThrow("null pooling must be handled gracefully — DeepClone(null) == null");

            var editor = new ConnectorInputEditor<double>(design);
            editor.Connector.CrossSourceAggregatingPooling.Should().BeNull();
            editor.Connector.PerSourceAggregatingPooling.Should().BeNull();
        }

        [Fact]
        public void ResetToDefaults_WithNullPoolingInDesign_DoesNotThrow()
        {
            var design = new ConnectorDesignInput<double>
            {
                Name = "x",
                Id = Guid.NewGuid(),
                DefaultCrossSourcePoolingStrategy = PoolingStrategy.Queue,
                DefaultCrossSourceAggregatingPooling = null,
                DefaultPerSourcePoolingStrategy = PoolingStrategy.Queue,
            };

            var editor = new ConnectorInputEditor<double>(design);
            Action act = editor.ResetToDefaults;
            act.Should().NotThrow();
        }
    }

    // =========================================================================
    // Helper types used across tests
    // =========================================================================

    public class SimplePoco
    {
        public int X { get; set; }
        public string? Y { get; set; }
    }

    public class NestedPoco
    {
        public int Value { get; set; }
        public SimplePoco? Inner { get; set; }
    }

    public class ReadonlyFieldClass(int id, string label)
    {
        private readonly int _id = id;
        private readonly string _label = label;

        public int GetId() => _id;
        public string GetLabel() => _label;
    }

    public class NoDefaultCtorClass(int value, string name)
    {
        public int Value { get; } = value;
        public string Name { get; } = name;
    }

    public class PlainClass
    {
        public int Num { get; set; }
    }

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
    public struct StructWithRef
    {
        public string? Name { get; set; }
    }

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
    public struct StructWithObject
    {
        public SimplePoco? Inner { get; set; }
    }

    public class Node
    {
        public int Value { get; set; }
        public Node? Next { get; set; }
    }

    public class TwoFieldObject
    {
        public SimplePoco? First { get; set; }
        public SimplePoco? Second { get; set; }
    }

    public class WithDelegateField
    {
        public Action? Handler { get; set; }
    }

    public class WithEvent
    {
        private Action? _someEvent;

        public event Action? SomeEvent
        {
            add => _someEvent += value;
            remove => _someEvent -= value;
        }

        public Delegate? GetEventDelegate() => _someEvent;
    }

    public class WithTypeField
    {
        public Type? StoredType { get; set; }
        public string? Name { get; set; }
    }

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
    public struct StructWithTypeField
    {
        public Type? StoredType { get; set; }
        public int Value { get; set; }
        public SimplePoco? Inner { get; set; }
    }

    // --- helpers for ReadonlyMutableFieldTests ---

    public class ContainerWithReadonlyRef(SimplePoco inner)
    {
        private readonly SimplePoco _inner = inner;

        public SimplePoco GetInner() => _inner;
    }

    // --- helpers for PolymorphicFieldTests ---

    public interface IShape { int Area { get; } }

    public class Circle : IShape
    {
        public int Radius { get; set; }
        public int Area => Radius * Radius;
    }

    public class ShapeHolder { public IShape? Shape { get; set; } }

    public abstract class Animal { public string? Name { get; set; } }

    public class Dog : Animal { public string? Breed { get; set; } }

    public class Zoo { public Animal? Resident { get; set; } }

    // --- helpers for InheritanceTests ---

    public class Base { public SimplePoco? BaseProp { get; set; } }

    public class Derived : Base { public SimplePoco? DerivedProp { get; set; } }

    // --- helpers for RecordCloningTests ---

    public record Point(int X, int Y);

    public record Line(Point Start, Point End);

    // --- helpers for BoxedValueTests ---

    private sealed class MutableModuloComparer : IEqualityComparer<int>
    {
        public int Modulo { get; set; } = 10;
        public bool Equals(int x, int y) => x % Modulo == y % Modulo;
        public int GetHashCode(int obj) => obj % Modulo;
    }

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
    public struct BoxedSelfReferencingStruct
    {
        public object? Self { get; set; }
        public int Value { get; set; }
    }

    public class BoxHolder { public object? Value { get; set; } }
}
