using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    // Standard member behavior
    // =========================================================================

    public class MemberBehaviorTests
    {
        [Fact]
        public void DeepClone_NonSerializedFields_AreLeftAtDefaultValues()
        {
            var original = new WithNonSerializedFields
            {
                Persistent = new SimplePoco { X = 1 },
                Transient = new SimplePoco { X = 2 },
                TransientNumber = 3,
                TransientNullable = 4
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Persistent.Should().NotBeSameAs(original.Persistent);
            clone.Persistent!.X.Should().Be(1);
            clone.Transient.Should().BeNull();
            clone.TransientNumber.Should().Be(0);
            clone.TransientNullable.Should().BeNull();
        }

        [Fact]
        public void DeepClone_NonSerializedStructField_IsLeftAtDefaultValue()
        {
            var original = new StructWithNonSerializedField
            {
                Persistent = new SimplePoco { X = 1 },
                Transient = new SimplePoco { X = 2 }
            };

            var clone = DeepCloner.DeepClone(original);

            clone.Persistent.Should().NotBeSameAs(original.Persistent);
            clone.Transient.Should().BeNull();
        }

        [Fact]
        public void DeepClone_NonSerializedEvent_DoesNotCopyInvocationList()
        {
            var original = new WithNonSerializedEvent();
            original.SomeEvent += static () => { };

            var clone = DeepCloner.DeepClone(original)!;

            original.GetEventDelegate().Should().NotBeNull();
            clone.GetEventDelegate().Should().BeNull();
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

        [Fact]
        public void DeepClone_HashSetWithIdentityHashedElements_RebuildsValidBuckets()
        {
            var element = new IdentityHashKey { Value = 42 };
            var original = new HashSet<IdentityHashKey> { element };

            var clone = DeepCloner.DeepClone(original)!;
            IdentityHashKey clonedElement = clone.Single();

            clone.Should().NotBeSameAs(original);
            clonedElement.Should().NotBeSameAs(element);
            clone.Contains(clonedElement).Should().BeTrue(
                "the clone must hash the cloned element rather than retain the source bucket hash");
        }

        [Fact]
        public void DeepClone_HashSetContainingItself_PreservesCycleAndLookup()
        {
            var original = new HashSet<object>(ReferenceEqualityComparer.Instance);
            original.Add(original);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Single().Should().BeSameAs(clone);
            clone.Contains(clone).Should().BeTrue();
            clone.Comparer.Should().BeSameAs(ReferenceEqualityComparer.Instance);
        }

        [Fact]
        public void DeepClone_DerivedDictionary_RehashesKeysAndClonesDerivedState()
        {
            var key = new IdentityHashKey { Value = 7 };
            var original = new DerivedIdentityDictionary(ReferenceEqualityComparer.Instance)
            {
                [key] = new SimplePoco { X = 10 },
                Metadata = new SimplePoco { X = 20 }
            };

            var clone = DeepCloner.DeepClone(original)!;
            IdentityHashKey clonedKey = clone.Keys.Single();

            clone.Should().BeOfType<DerivedIdentityDictionary>();
            clonedKey.Should().NotBeSameAs(key);
            clone.ContainsKey(clonedKey).Should().BeTrue();
            clone[clonedKey].Should().NotBeSameAs(original[key]);
            clone.Metadata.Should().NotBeSameAs(original.Metadata);
            clone.Metadata!.X.Should().Be(20);
            clone.Transient.Should().BeNull(
                "ignored derived state must be reset even though the collection constructor ran");
            clone.Comparer.Should().BeSameAs(original.Comparer);
        }

        [Fact]
        public void DeepClone_DerivedHashSet_RehashesElementsAndClonesDerivedState()
        {
            var element = new IdentityHashKey { Value = 9 };
            var original = new DerivedIdentityHashSet(ReferenceEqualityComparer.Instance)
            {
                Metadata = new SimplePoco { X = 30 }
            };
            original.Add(element);

            var clone = DeepCloner.DeepClone(original)!;
            IdentityHashKey clonedElement = clone.Single();

            clone.Should().BeOfType<DerivedIdentityHashSet>();
            clonedElement.Should().NotBeSameAs(element);
            clone.Contains(clonedElement).Should().BeTrue();
            clone.Metadata.Should().NotBeSameAs(original.Metadata);
            clone.Metadata!.X.Should().Be(30);
            clone.Comparer.Should().BeSameAs(original.Comparer);
        }

        // Constructors are not inherited, so the clone is built through the subclass's own
        // parameterless constructor — which supplies an equivalent comparer of its own.
        [Fact]
        public void DeepClone_DictionarySubclassWithBaseSuppliedComparer_PreservesLookup()
        {
            var original = new CaseInsensitiveDictionary { ["Alpha"] = 1 };

            CaseInsensitiveDictionary clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone["ALPHA"].Should().Be(1);
        }

        // A comparer that carries configuration cannot be assumed equivalent just because the type
        // matches, so the clone is rejected instead of silently changing lookup semantics.
        [Fact]
        public void DeepClone_DictionarySubclassWithStatefulComparer_FailsFast()
        {
            var original = new TrimmingDictionary { ["a"] = 1 };

            Action act = () => DeepCloner.DeepClone(original);

            act.Should().Throw<NotSupportedException>().WithMessage("*cannot preserve its comparer*");
        }

        // An int parameter is only a capacity when the constructor names it one. A domain int must
        // not be fed the element count, so the parameterless constructor is used instead.
        [Fact]
        public void DeepClone_DictionarySubclassWithDomainIntConstructor_UsesParameterlessConstructor()
        {
            var original = new TaggedDictionary(42) { ["a"] = 1 };

            TaggedDictionary clone = DeepCloner.DeepClone(original)!;

            clone.Tag.Should().Be(42);
            clone["a"].Should().Be(1);
        }

        // With no parameterless constructor there is no way to build a valid empty shell without
        // running domain logic against a fabricated argument, so the clone is rejected by name.
        [Fact]
        public void DeepClone_DictionarySubclassWithOnlyADomainIntConstructor_FailsFast()
        {
            var original = new TagOnlyDictionary(42) { ["a"] = 1 };

            Action act = () => DeepCloner.DeepClone(original);

            act.Should().Throw<NotSupportedException>()
                .WithMessage("*TagOnlyDictionary*no constructor that can create a valid empty clone*");
        }

        [Fact]
        public void DeepClone_Queue_PreservesOrderAndClonesElements()
        {
            var first = new SimplePoco { X = 1 };
            var second = new SimplePoco { X = 2 };
            var original = new Queue<SimplePoco>([first, second]);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Select(item => item.X).Should().Equal(1, 2);
            clone.ElementAt(0).Should().NotBeSameAs(first);
            clone.ElementAt(1).Should().NotBeSameAs(second);
        }

        [Fact]
        public void DeepClone_QueueContainingItself_PreservesCycle()
        {
            var original = new Queue<object>();
            original.Enqueue(original);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Single().Should().BeSameAs(clone);
        }

        [Fact]
        public void DeepClone_Stack_PreservesTopToBottomOrderAndCycle()
        {
            var item = new SimplePoco { X = 1 };
            var original = new Stack<object>();
            original.Push(item);
            original.Push(original);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Pop().Should().BeSameAs(clone);
            var clonedItem = clone.Pop().Should().BeOfType<SimplePoco>().Subject;
            clonedItem.Should().NotBeSameAs(item);
            clonedItem.X.Should().Be(1);
        }

        [Fact]
        public void DeepClone_DerivedList_PreservesRuntimeTypeAndDerivedState()
        {
            var item = new SimplePoco { X = 1 };
            var original = new DerivedPocoList(4)
            {
                Metadata = new SimplePoco { X = 2 }
            };
            original.Add(item);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().BeOfType<DerivedPocoList>();
            clone.Single().Should().NotBeSameAs(item);
            clone.Single().X.Should().Be(1);
            clone.Metadata.Should().NotBeSameAs(original.Metadata);
            clone.Metadata!.X.Should().Be(2);
        }

        // An int parameter is only a capacity when the constructor names it one. A domain int must
        // not be fed the element count, so the parameterless constructor is used instead.
        [Fact]
        public void DeepClone_ListSubclassWithDomainIntConstructor_UsesParameterlessConstructor()
        {
            var original = new TaggedList(42) { 1, 2, 3 };

            TaggedList clone = DeepCloner.DeepClone(original)!;

            clone.Tag.Should().Be(42);
            clone.Should().Equal(1, 2, 3);
        }

        // With no parameterless constructor there is no way to build a valid empty shell without
        // running domain logic against a fabricated argument, so the clone is rejected by name.
        [Fact]
        public void DeepClone_ListSubclassWithOnlyADomainIntConstructor_FailsFast()
        {
            var original = new TagOnlyList(42) { 1 };

            Action act = () => DeepCloner.DeepClone(original);

            act.Should().Throw<NotSupportedException>()
                .WithMessage("*TagOnlyList*no constructor that can create a valid empty clone*");
        }

        [Fact]
        public void DeepClone_ConcurrentDictionary_RehashesKeysAndPreservesComparer()
        {
            var key = new IdentityHashKey { Value = 11 };
            var original = new ConcurrentDictionary<IdentityHashKey, SimplePoco>(
                ReferenceEqualityComparer.Instance);
            original[key] = new SimplePoco { X = 12 };

            var clone = DeepCloner.DeepClone(original)!;
            IdentityHashKey clonedKey = clone.Keys.Single();

            clone.Should().NotBeSameAs(original);
            clonedKey.Should().NotBeSameAs(key);
            clone.ContainsKey(clonedKey).Should().BeTrue();
            clone[clonedKey].Should().NotBeSameAs(original[key]);
            clone.Comparer.Should().BeSameAs(ReferenceEqualityComparer.Instance);
        }

        [Fact]
        public void DeepClone_ConcurrentQueueContainingItself_PreservesCycle()
        {
            var original = new ConcurrentQueue<object>();
            original.Enqueue(original);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Single().Should().BeSameAs(clone);
        }

        [Fact]
        public void DeepClone_ConcurrentStack_PreservesOrderAndClonesElements()
        {
            var first = new SimplePoco { X = 1 };
            var second = new SimplePoco { X = 2 };
            var original = new ConcurrentStack<SimplePoco>();
            original.Push(first);
            original.Push(second);

            var clone = DeepCloner.DeepClone(original)!;

            clone.Select(item => item.X).Should().Equal(2, 1);
            clone.ElementAt(0).Should().NotBeSameAs(second);
            clone.ElementAt(1).Should().NotBeSameAs(first);
        }

        [Fact]
        public void DeepClone_ConcurrentBag_ClonesSnapshotElements()
        {
            var item = new SimplePoco { X = 5 };
            var original = new ConcurrentBag<SimplePoco> { item };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().ContainSingle();
            clone.Single().Should().NotBeSameAs(item);
            clone.Single().X.Should().Be(5);
        }

        [Fact]
        public void DeepClone_SortedDictionary_RebuildsKeysAndPreservesComparer()
        {
            var comparer = new PocoValueComparer();
            var key = new SimplePoco { X = 3 };
            var original = new SortedDictionary<SimplePoco, SimplePoco>(comparer)
            {
                [key] = new SimplePoco { X = 4 }
            };

            var clone = DeepCloner.DeepClone(original)!;
            SimplePoco clonedKey = clone.Keys.Single();

            clonedKey.Should().NotBeSameAs(key);
            clone.ContainsKey(clonedKey).Should().BeTrue();
            clone[clonedKey].Should().NotBeSameAs(original[key]);
            clone.Comparer.Should().BeSameAs(comparer);
        }

        [Fact]
        public void DeepClone_SortedList_RebuildsKeysAndValues()
        {
            var comparer = new PocoValueComparer();
            var key = new SimplePoco { X = 7 };
            var original = new SortedList<SimplePoco, SimplePoco>(comparer)
            {
                [key] = new SimplePoco { X = 8 }
            };

            var clone = DeepCloner.DeepClone(original)!;
            SimplePoco clonedKey = clone.Keys.Single();

            clonedKey.Should().NotBeSameAs(key);
            clone.ContainsKey(clonedKey).Should().BeTrue();
            clone.Values.Single().Should().NotBeSameAs(original.Values.Single());
            clone.Comparer.Should().BeSameAs(comparer);
        }

        [Fact]
        public void DeepClone_SortedSet_RebuildsElementsAndPreservesComparer()
        {
            var comparer = new PocoValueComparer();
            var item = new SimplePoco { X = 9 };
            var original = new SortedSet<SimplePoco>(comparer) { item };

            var clone = DeepCloner.DeepClone(original)!;
            SimplePoco clonedItem = clone.Single();

            clonedItem.Should().NotBeSameAs(item);
            clone.Contains(clonedItem).Should().BeTrue();
            clone.Comparer.Should().BeSameAs(comparer);
        }

        [Fact]
        public void DeepClone_ImmutableArray_ClonesMutableElements()
        {
            var item = new SimplePoco { X = 13 };
            ImmutableArray<SimplePoco> original = [item];

            ImmutableArray<SimplePoco> clone = DeepCloner.DeepClone(original);

            clone.Should().HaveCount(1);
            clone[0].Should().NotBeSameAs(item);
            clone[0].X.Should().Be(13);
        }

        // Immutable collections are containers, not values: the container cannot be mutated
        // through the clone, but its elements can, so they are rebuilt from cloned elements.
        [Fact]
        public void DeepClone_ImmutableList_ClonesMutableElements()
        {
            ImmutableList<SimplePoco> original = [new SimplePoco { X = 14 }];

            ImmutableList<SimplePoco> clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Should().ContainSingle();
            clone[0].Should().NotBeSameAs(original[0]);
            clone[0].X.Should().Be(14);
        }

        [Fact]
        public void DeepClone_ImmutableStackAndQueue_PreserveOrder()
        {
            ImmutableStack<int> stack = ImmutableStack.CreateRange([1, 2, 3]);
            ImmutableQueue<int> queue = ImmutableQueue.CreateRange([1, 2, 3]);

            DeepCloner.DeepClone(stack).Should().Equal(stack);
            DeepCloner.DeepClone(queue).Should().Equal(queue);
        }

        [Fact]
        public void DeepClone_ImmutableDictionary_ClonesValuesAndPreservesComparer()
        {
            ImmutableDictionary<string, SimplePoco> original = ImmutableDictionary
                .CreateRange(
                    StringComparer.OrdinalIgnoreCase,
                    [KeyValuePair.Create("a", new SimplePoco { X = 15 })]);

            ImmutableDictionary<string, SimplePoco> clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.KeyComparer.Should().BeSameAs(StringComparer.OrdinalIgnoreCase);
            clone["A"].Should().NotBeSameAs(original["a"]);
            clone["A"].X.Should().Be(15);
        }

        [Fact]
        public void DeepClone_ImmutableHashSet_RehashesClonedElements()
        {
            var element = new IdentityHashKey { Value = 16 };
            ImmutableHashSet<IdentityHashKey> original = [element];

            ImmutableHashSet<IdentityHashKey> clone = DeepCloner.DeepClone(original)!;

            IdentityHashKey clonedElement = clone.Single();
            clonedElement.Should().NotBeSameAs(element);
            clone.Contains(clonedElement).Should().BeTrue();
        }

        [Fact]
        public void DeepClone_FrozenCollections_ClonesEntriesAndPreservesComparer()
        {
            FrozenDictionary<string, SimplePoco> dictionary =
                new Dictionary<string, SimplePoco> { ["a"] = new() { X = 17 } }
                    .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            FrozenSet<SimplePoco> set = new[] { new SimplePoco { X = 18 } }.ToFrozenSet();

            FrozenDictionary<string, SimplePoco> dictionaryClone = DeepCloner.DeepClone(dictionary)!;
            FrozenSet<SimplePoco> setClone = DeepCloner.DeepClone(set)!;

            dictionaryClone.Should().NotBeSameAs(dictionary);
            dictionaryClone.Comparer.Should().BeSameAs(StringComparer.OrdinalIgnoreCase);
            dictionaryClone["A"].Should().NotBeSameAs(dictionary["a"]);
            dictionaryClone["A"].X.Should().Be(17);

            setClone.Should().NotBeSameAs(set);
            setClone.Single().Should().NotBeSameAs(set.Single());
            setClone.Single().X.Should().Be(18);
        }

        // A back-edge that passes through an already-registered object is fine: the holder is
        // registered before its field is cloned, so the immutable list can close the cycle.
        [Fact]
        public void DeepClone_CycleEnteredThroughHolder_PreservesCycle()
        {
            var original = new ImmutableListHolder();
            original.Items = [original];

            ImmutableListHolder clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Items.Should().NotBeSameAs(original.Items);
            clone.Items.Single().Should().BeSameAs(clone);
        }

        // Entered from the collection itself there is nothing to register first: the instance
        // cannot exist before its contents do.
        [Fact]
        public void DeepClone_CycleEnteredThroughImmutableList_FailsFast()
        {
            var holder = new ImmutableListHolder();
            holder.Items = [holder];

            Action act = () => DeepCloner.DeepClone(holder.Items);

            act.Should().Throw<NotSupportedException>().WithMessage("*cycle*");
        }

        [Fact]
        public void DeepClone_LinkedListAndExternalNode_PreservesNodeOwnershipAlias()
        {
            var item = new SimplePoco { X = 16 };
            var list = new LinkedList<SimplePoco>();
            LinkedListNode<SimplePoco> node = list.AddLast(item);
            var original = new LinkedListHolder(list, node);

            var clone = DeepCloner.DeepClone(original)!;

            clone.List.Should().NotBeSameAs(list);
            clone.Node.Should().NotBeSameAs(node);
            clone.Node.Should().BeSameAs(clone.List!.First);
            clone.Node!.List.Should().BeSameAs(clone.List);
            clone.Node.Value.Should().NotBeSameAs(item);
        }

        [Fact]
        public void DeepClone_ReadOnlyCollection_PreservesBackingListAlias()
        {
            var list = new List<SimplePoco> { new() { X = 17 } };
            var original = new ReadOnlyCollectionHolder(
                list,
                new ReadOnlyCollection<SimplePoco>(list));

            var clone = DeepCloner.DeepClone(original)!;

            clone.List.Should().NotBeSameAs(list);
            clone.View.Should().NotBeSameAs(original.View);
            clone.View![0].Should().BeSameAs(clone.List![0]);
            clone.List.Add(new SimplePoco { X = 18 });
            clone.View.Should().HaveCount(2,
                "the cloned wrapper must reference the cloned backing list");
        }

        [Fact] // A seeding constructor leaves its own elements in front of the cloned ones
        public void DeepClone_DerivedCollectionWithSeedingConstructor_FailsFast()
        {
            var list = new SeedingList { 5 };
            var queue = new SeedingQueue();
            queue.Enqueue(5);
            var dictionary = new SeedingDictionary { ["a"] = 5 };
            var set = new SeedingHashSet { 5 };

            Action cloneList = () => DeepCloner.DeepClone(list);
            Action cloneQueue = () => DeepCloner.DeepClone(queue);
            Action cloneDictionary = () => DeepCloner.DeepClone(dictionary);
            Action cloneSet = () => DeepCloner.DeepClone(set);

            cloneList.Should().Throw<NotSupportedException>().WithMessage("*seeds content*");
            cloneQueue.Should().Throw<NotSupportedException>().WithMessage("*seeds content*");
            cloneDictionary.Should().Throw<NotSupportedException>().WithMessage("*seeds content*");
            cloneSet.Should().Throw<NotSupportedException>().WithMessage("*seeds content*");
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
        [Fact]
        public void DeepClone_DeepLinkedList_DoesNotOverflowStack()
        {
            const int Depth = 20_000;
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

        [Fact]
        public void DeepClone_DeepArrayChain_DoesNotOverflowStack()
        {
            const int Depth = 5_000;
            var root = new object[1];
            object[] current = root;
            for (var i = 1; i < Depth; i++)
            {
                var next = new object[1];
                current[0] = next;
                current = next;
            }

            object[] clone = DeepCloner.DeepClone(root)!;

            object[] originalCursor = root;
            object[] cloneCursor = clone;
            for (var i = 1; i < Depth; i++)
            {
                cloneCursor.Should().NotBeSameAs(originalCursor);
                originalCursor = (object[])originalCursor[0];
                cloneCursor = (object[])cloneCursor[0];
            }
            cloneCursor.Should().NotBeSameAs(originalCursor);
        }

        // Past the recursion guard a clone is handed out before its fields are populated. A
        // collection that hashes or orders by element state must wait for it, or the entry is
        // filed under default field values — silently dropping or hiding it.
        [Theory]
        [InlineData(20)]  // below the guard — entries are inserted while cloning
        [InlineData(200)] // above it — entries are inserted after the graph is complete
        public void DeepClone_DeepChainOfDictionaries_PreservesEveryEntry(int levels)
        {
            DictionaryChainNode original = BuildDictionaryChain(levels);

            DictionaryChainNode clone = DeepCloner.DeepClone(original)!;

            var walked = 0;
            DictionaryChainNode? cursor = clone;
            while (cursor is not null && cursor.Children.Count > 0)
            {
                cursor.Children.Should().HaveCount(2, "no entry may collapse into another");
                foreach (KeyRecord key in cursor.Children.Keys.ToList())
                {
                    cursor.Children.ContainsKey(key).Should()
                        .BeTrue("every entry must be reachable through its own key");
                }

                cursor = cursor.Children.Values.Single(child => child is not null);
                walked++;
            }

            walked.Should().Be(levels - 1);
        }

        [Theory]
        [InlineData(20)]
        [InlineData(200)]
        public void DeepClone_DeepChainOfHashSets_PreservesEveryElement(int levels)
        {
            HashSetChainNode original = BuildHashSetChain(levels);

            HashSetChainNode clone = DeepCloner.DeepClone(original)!;

            var walked = 0;
            for (HashSetChainNode? cursor = clone; cursor is not null; cursor = cursor.Next)
            {
                cursor.Tags.Should().HaveCount(2, "no element may collapse into another");
                foreach (KeyRecord tag in cursor.Tags)
                    cursor.Tags.Contains(tag).Should().BeTrue("the element must hash into its own bucket");

                walked++;
            }

            walked.Should().Be(levels);
        }

        [Theory]
        [InlineData(20)]
        [InlineData(200)]
        public void DeepClone_DeepChainOfSortedSets_PreservesEveryElement(int levels)
        {
            SortedSetChainNode original = BuildSortedSetChain(levels);

            SortedSetChainNode clone = DeepCloner.DeepClone(original)!;

            var walked = 0;
            for (SortedSetChainNode? cursor = clone; cursor is not null; cursor = cursor.Next)
            {
                cursor.Tags.Should().HaveCount(2, "elements comparing equal would collapse");
                cursor.Tags.Select(tag => tag.X).Should().BeInAscendingOrder();
                walked++;
            }

            walked.Should().Be(levels);
        }

        // Every level carries a set of sets, so some level is guaranteed to straddle the
        // recursion guard: its outer set is filled while the inner sets are still queued.
        [Fact]
        public void DeepClone_DeepGraphWithNestedHashSets_FillsInnerSetsFirst()
        {
            const int Levels = 200;
            NestedSetChainNode? original = null;
            for (var i = 0; i < Levels; i++)
            {
                var node = new NestedSetChainNode { Next = original };
                node.Sets.Add([new KeyRecord((2 * i) + 1)]);
                node.Sets.Add([new KeyRecord((2 * i) + 2)]);
                original = node;
            }

            NestedSetChainNode clone = DeepCloner.DeepClone(original)!;

            var walked = 0;
            for (NestedSetChainNode? cursor = clone; cursor is not null; cursor = cursor.Next)
            {
                cursor.Sets.Should().HaveCount(2, "empty inner sets would hash alike and collapse");
                foreach (HashSet<KeyRecord> inner in cursor.Sets)
                {
                    inner.Should().ContainSingle();
                    cursor.Sets.Contains(inner).Should()
                        .BeTrue("the outer set must hash the filled inner set");
                }

                walked++;
            }

            walked.Should().Be(Levels);
        }

        private static DictionaryChainNode BuildDictionaryChain(int levels)
        {
            var nodes = new List<DictionaryChainNode>();
            for (var i = 0; i < levels; i++)
                nodes.Add(new DictionaryChainNode());

            for (var i = 0; i < levels - 1; i++)
            {
                nodes[i].Children[new KeyRecord((2 * i) + 1)] = nodes[i + 1];
                nodes[i].Children[new KeyRecord((2 * i) + 2)] = null;
            }

            return nodes[0];
        }

        private static HashSetChainNode BuildHashSetChain(int levels)
        {
            var nodes = new List<HashSetChainNode>();
            for (var i = 0; i < levels; i++)
            {
                var node = new HashSetChainNode();
                node.Tags.Add(new KeyRecord((2 * i) + 1));
                node.Tags.Add(new KeyRecord((2 * i) + 2));
                nodes.Add(node);
            }

            for (var i = 0; i < levels - 1; i++)
                nodes[i].Next = nodes[i + 1];

            return nodes[0];
        }

        private static SortedSetChainNode BuildSortedSetChain(int levels)
        {
            var nodes = new List<SortedSetChainNode>();
            for (var i = 0; i < levels; i++)
            {
                var node = new SortedSetChainNode();
                node.Tags.Add(new SimplePoco { X = (2 * i) + 1 });
                node.Tags.Add(new SimplePoco { X = (2 * i) + 2 });
                nodes.Add(node);
            }

            for (var i = 0; i < levels - 1; i++)
                nodes[i].Next = nodes[i + 1];

            return nodes[0];
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
    // Runtime-managed and resource-owning objects — shared by reference
    // =========================================================================

    public class RuntimeManagedTypeTests
    {
        [Fact]
        public void DeepClone_KnownRuntimeManagedObjects_ReturnsSameReferences()
        {
            Task<int> task = Task.FromResult(42);
            var weakReference = new WeakReference<object>(new object());
            var lazy = new Lazy<SimplePoco>(() => new SimplePoco { X = 1 });
            using var cancellation = new CancellationTokenSource();
            using var stream = new MemoryStream([1, 2, 3]);

            DeepCloner.DeepClone(DBNull.Value).Should().BeSameAs(DBNull.Value);
            DeepCloner.DeepClone(TimeZoneInfo.Utc).Should().BeSameAs(TimeZoneInfo.Utc);
            DeepCloner.DeepClone(task).Should().BeSameAs(task);
            DeepCloner.DeepClone(weakReference).Should().BeSameAs(weakReference);
            DeepCloner.DeepClone(lazy).Should().BeSameAs(lazy);
            DeepCloner.DeepClone(cancellation).Should().BeSameAs(cancellation);
            DeepCloner.DeepClone(stream).Should().BeSameAs(stream);
            DeepCloner.DeepClone(StringComparer.OrdinalIgnoreCase)
                .Should().BeSameAs(StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void DeepClone_FrameworkComparers_ReturnSameReferences()
        {
            DeepCloner.DeepClone(EqualityComparer<string>.Default)
                .Should().BeSameAs(EqualityComparer<string>.Default);
            DeepCloner.DeepClone(Comparer<string>.Default)
                .Should().BeSameAs(Comparer<string>.Default);
            DeepCloner.DeepClone(ReferenceEqualityComparer.Instance)
                .Should().BeSameAs(ReferenceEqualityComparer.Instance);
        }

        // Implementing a comparer interface says nothing about immutability. A data object that also
        // carries a comparison rule is data, and the caller asked for an independent copy.
        [Fact]
        public void DeepClone_MutableDataImplementingComparer_IsCloned()
        {
            var original = new SortRuleHolder { Rule = new SortRule { Descending = true } };

            SortRuleHolder clone = DeepCloner.DeepClone(original)!;

            clone.Rule.Should().NotBeSameAs(original.Rule);
            clone.Rule!.Descending.Should().BeTrue();

            clone.Rule.Descending = false;
            original.Rule!.Descending.Should().BeTrue("the clone must not alias the source");
        }

        [Fact]
        public void DeepClone_FinalizableObject_ReturnsSameReference()
        {
            var original = new FinalizableResource();

            var clone = DeepCloner.DeepClone(original);

            clone.Should().BeSameAs(original,
                "duplicating a finalizable owner could execute cleanup twice");
            GC.KeepAlive(original);
        }

        [Fact] // Being disposable is not by itself a reason to share — the caller asked for a copy
        public void DeepClone_PlainDisposable_IsDeepCloned()
        {
            using var original = new DisposableData { Payload = new SimplePoco { X = 5 } };

            using DisposableData clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.Payload.Should().NotBeSameAs(original.Payload);
            clone.Payload!.X.Should().Be(5);
        }

        [Fact] // Mutating the clone must not reach the original
        public void DeepClone_HolderWithPlainDisposable_ClonesIndependently()
        {
            using var state = new DisposableData { Payload = new SimplePoco { X = 1 } };
            var original = new DisposableHolder { State = state };

            var clone = DeepCloner.DeepClone(original)!;

            clone.State.Should().NotBeSameAs(state);
            clone.State!.Payload!.X = 99;
            state.Payload!.X.Should().Be(1);
            clone.State.Dispose();
        }

        [Fact]
        public void DeepClone_HolderWithRuntimeObject_SharesResourceAndClonesData()
        {
            using var stream = new MemoryStream([1, 2, 3]);
            var original = new RuntimeManagedHolder
            {
                RuntimeObject = stream,
                Data = new SimplePoco { X = 42 }
            };

            var clone = DeepCloner.DeepClone(original)!;

            clone.Should().NotBeSameAs(original);
            clone.RuntimeObject.Should().BeSameAs(stream);
            clone.Data.Should().NotBeSameAs(original.Data);
            clone.Data!.X.Should().Be(42);
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

    public sealed class RuntimeManagedHolder
    {
        public object? RuntimeObject { get; set; }
        public SimplePoco? Data { get; set; }
    }

    public sealed class LinkedListHolder(
        LinkedList<SimplePoco> list,
        LinkedListNode<SimplePoco> node)
    {
        private readonly LinkedList<SimplePoco> _list = list;
        private readonly LinkedListNode<SimplePoco> _node = node;

        public LinkedList<SimplePoco> List => _list;
        public LinkedListNode<SimplePoco> Node => _node;
    }

    public sealed class ReadOnlyCollectionHolder(
        List<SimplePoco> list,
        ReadOnlyCollection<SimplePoco> view)
    {
        private readonly List<SimplePoco> _list = list;
        private readonly ReadOnlyCollection<SimplePoco> _view = view;

        public List<SimplePoco> List => _list;
        public ReadOnlyCollection<SimplePoco> View => _view;
    }

    [SuppressMessage("Performance", "CA1821:Remove empty finalizers",
        Justification = "Exercises the clone policy for arbitrary finalizable resource owners.")]
    public sealed class FinalizableResource
    {
        ~FinalizableResource()
        {
        }
    }

    // Mutable data that also carries a comparison rule — data, not a stateless framework comparer.
    public sealed class SortRule : IComparer<string>
    {
        public bool Descending { get; set; }

        public int Compare(string? x, string? y)
            => Descending ? string.CompareOrdinal(y, x) : string.CompareOrdinal(x, y);
    }

    public sealed class SortRuleHolder
    {
        public SortRule? Rule { get; set; }
    }

    // Disposable, but owns no external resource and has no finalizer — ordinary data.
    public sealed class DisposableData : IDisposable
    {
        public SimplePoco? Payload { get; set; }

        public void Dispose() => GC.SuppressFinalize(this);
    }

    public sealed class DisposableHolder
    {
        public DisposableData? State { get; set; }
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

    public sealed class WithNonSerializedFields
    {
        public SimplePoco? Persistent { get; set; }

        [field: NonSerialized]
        public SimplePoco? Transient { get; set; }

        [field: NonSerialized]
        public int TransientNumber { get; set; }

        [field: NonSerialized]
        public int? TransientNullable { get; set; }
    }

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
    public struct StructWithNonSerializedField
    {
        public SimplePoco? Persistent { get; set; }

        [field: NonSerialized]
        public SimplePoco? Transient { get; set; }
    }

    public sealed class WithNonSerializedEvent
    {
        [field: NonSerialized]
        public event Action? SomeEvent;

        public Delegate? GetEventDelegate() => SomeEvent;
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

    private sealed class PocoValueComparer : IComparer<SimplePoco>
    {
        public int Compare(SimplePoco? x, SimplePoco? y)
            => Nullable.Compare(x?.X, y?.X);
    }

    public sealed class IdentityHashKey
    {
        public int Value { get; set; }
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }

    public sealed class DerivedIdentityDictionary(IEqualityComparer<IdentityHashKey> comparer)
        : Dictionary<IdentityHashKey, SimplePoco>(comparer)
    {
        public SimplePoco? Metadata { get; set; }

        [field: NonSerialized]
        public SimplePoco? Transient { get; set; } = new() { X = -1 };
    }

    public sealed class DerivedIdentityHashSet(IEqualityComparer<IdentityHashKey> comparer)
        : HashSet<IdentityHashKey>(comparer)
    {
        public SimplePoco? Metadata { get; set; }
    }

    // Stateless, so any instance behaves exactly like any other.
    public sealed class OrdinalIgnoreCaseKeys : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);

        public int GetHashCode(string obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
    }

    // Carries configuration, so two instances of it are not interchangeable.
    public sealed class TrimmingKeys(bool trim) : IEqualityComparer<string>
    {
        private readonly bool _trim = trim;

        public bool Equals(string? x, string? y)
            => string.Equals(Normalize(x), Normalize(y), StringComparison.Ordinal);

        public int GetHashCode(string obj) => Normalize(obj)?.GetHashCode(StringComparison.Ordinal) ?? 0;

        private string? Normalize(string? value) => _trim ? value?.Trim() : value;
    }

    public sealed class CaseInsensitiveDictionary : Dictionary<string, int>
    {
        public CaseInsensitiveDictionary() : base(new OrdinalIgnoreCaseKeys())
        {
        }
    }

    public sealed class TrimmingDictionary : Dictionary<string, int>
    {
        public TrimmingDictionary() : base(new TrimmingKeys(trim: true))
        {
        }
    }

    // The int is a domain value, not a capacity — feeding it the element count would run this
    // validation against a fabricated argument.
    public sealed class TaggedDictionary : Dictionary<string, int>
    {
        public TaggedDictionary()
        {
        }

        public TaggedDictionary(int tag)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tag);
            Tag = tag;
        }

        public int Tag { get; set; }
    }

    public sealed class TagOnlyDictionary : Dictionary<string, int>
    {
        public TagOnlyDictionary(int tag)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tag);
            Tag = tag;
        }

        public int Tag { get; set; }
    }

    public sealed class DerivedPocoList(int capacity) : List<SimplePoco>(capacity)
    {
        public SimplePoco? Metadata { get; set; }
    }

    // The int is a domain value, not a capacity.
    public sealed class TaggedList : List<int>
    {
        public TaggedList()
        {
        }

        public TaggedList(int tag)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tag);
            Tag = tag;
        }

        public int Tag { get; set; }
    }

    public sealed class TagOnlyList : List<int>
    {
        public TagOnlyList(int tag)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tag);
            Tag = tag;
        }

        public int Tag { get; set; }
    }

    public sealed class ImmutableListHolder
    {
        public ImmutableList<object> Items { get; set; } = [];
    }

    // Deep-graph payloads whose keys and elements hash or order by field state, so an
    // unpopulated clone would be filed under default values instead of its own.
    public sealed record KeyRecord(int Id);

    public sealed class DictionaryChainNode
    {
        public Dictionary<KeyRecord, DictionaryChainNode?> Children { get; } = [];
    }

    public sealed class HashSetChainNode
    {
        public HashSet<KeyRecord> Tags { get; } = [];

        public HashSetChainNode? Next { get; set; }
    }

    public sealed class SortedSetChainNode
    {
        public SortedSet<SimplePoco> Tags { get; } = new(new PocoValueComparer());

        public SortedSetChainNode? Next { get; set; }
    }

    public sealed class NestedSetChainNode
    {
        public NestedSetChainNode? Next { get; set; }

        public HashSet<HashSet<KeyRecord>> Sets { get; } = new(HashSet<KeyRecord>.CreateSetComparer());
    }

    // Derived collections whose constructors seed content — every constructor the clone
    // factories can pick must be covered, otherwise the guard is bypassed.
    public sealed class SeedingList : List<int>
    {
        public SeedingList() => Add(-1);
        public SeedingList(int capacity) : base(capacity) => Add(-1);
    }

    public sealed class SeedingQueue : Queue<int>
    {
        public SeedingQueue() => Enqueue(-1);
        public SeedingQueue(int capacity) : base(capacity) => Enqueue(-1);
    }

    public sealed class SeedingDictionary : Dictionary<string, int>
    {
        public SeedingDictionary() => Add("seed", -1);

        public SeedingDictionary(int capacity, IEqualityComparer<string>? comparer)
            : base(capacity, comparer) => Add("seed", -1);
    }

    public sealed class SeedingHashSet : HashSet<int>
    {
        public SeedingHashSet() => Add(-1);

        public SeedingHashSet(int capacity, IEqualityComparer<int>? comparer)
            : base(capacity, comparer) => Add(-1);
    }

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types")]
    public struct BoxedSelfReferencingStruct
    {
        public object? Self { get; set; }
        public int Value { get; set; }
    }

    public class BoxHolder { public object? Value { get; set; } }
}
