using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using ApplicationBuilderHelpers.Models;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Direct initializer-default value-semantics tests shared by global-option
/// promotion and help: arrays and exact <see cref="List{T}"/> instances compare
/// element-wise in order, while opaque, custom, lazy, and unordered collections
/// fail closed instead of being enumerated or compared by reference.
/// </summary>
public sealed class InitializerValueEqualityTests
{
    [Fact]
    public void NullInitializers_CompareEqual()
    {
        Assert.True(InitializerValueEquality.ValuesEqual(null, null));
    }

    [Fact]
    public void EmptyMaterializedLists_CompareEqual()
    {
        Assert.True(InitializerValueEquality.ValuesEqual(new List<string>(), new List<string>()));
    }

    [Fact]
    public void MaterializedListsWithEqualElements_CompareEqual()
    {
        Assert.True(InitializerValueEquality.ValuesEqual(
            new List<string> { "a", "b" },
            new List<string> { "a", "b" }));
        Assert.True(InitializerValueEquality.ValuesEqual(
            new List<int> { 1, 2 },
            new List<int> { 1, 2 }));
    }

    [Fact]
    public void MaterializedListsBehindInterfaceTypedVariables_CompareEqual()
    {
        IList<string> first = new List<string> { "a" };
        IEnumerable<string> second = new List<string> { "a" };

        Assert.True(InitializerValueEquality.ValuesEqual(first, second));
    }

    [Fact]
    public void ArraysWithEqualElements_CompareEqual()
    {
        Assert.True(InitializerValueEquality.ValuesEqual(new[] { "a", "b" }, new[] { "a", "b" }));
        Assert.True(InitializerValueEquality.ValuesEqual(Array.Empty<int>(), Array.Empty<int>()));
    }

    [Fact]
    public void ArrayAndMaterializedListWithEqualElements_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new[] { "a", "b" },
            new List<string> { "a", "b" }));
        Assert.False(InitializerValueEquality.ValuesEqual(
            new List<string> { "a", "b" },
            new[] { "a", "b" }));
    }

    [Fact]
    public void MaterializedListsWithDifferentCounts_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new List<string> { "a" },
            new List<string> { "a", "b" }));
    }

    [Fact]
    public void MaterializedListsWithDifferentElements_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new List<string> { "a" },
            new List<string> { "b" }));
    }

    [Fact]
    public void MaterializedListsWithReorderedElements_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new List<string> { "a", "b" },
            new List<string> { "b", "a" }));
    }

    [Fact]
    public void MaterializedListAndNull_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(new List<string>(), null));
        Assert.False(InitializerValueEquality.ValuesEqual(null, new List<string>()));
    }

    [Fact]
    public void MaterializedListsWithDifferentElementTypes_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(new List<string>(), new List<int>()));
        Assert.False(InitializerValueEquality.ValuesEqual(new List<string> { "a" }, new List<object> { "a" }));
    }

    [Fact]
    public void ScalarStrings_CompareByValue()
    {
        Assert.True(InitializerValueEquality.ValuesEqual("a", "a"));
        Assert.False(InitializerValueEquality.ValuesEqual("a", "b"));
    }

    [Fact]
    public void EqualEnumValues_CompareEqual()
    {
        Assert.True(InitializerValueEquality.ValuesEqual(DayOfWeek.Monday, DayOfWeek.Monday));
        Assert.False(InitializerValueEquality.ValuesEqual(DayOfWeek.Monday, DayOfWeek.Tuesday));
    }

    [Fact]
    public void CustomScalarsWithValueEquality_CompareEqual()
    {
        Assert.True(InitializerValueEquality.ValuesEqual(new CustomScalar(1), new CustomScalar(1)));
        Assert.False(InitializerValueEquality.ValuesEqual(new CustomScalar(1), new CustomScalar(2)));
    }

    [Fact]
    public void HashSetsWithEqualElements_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new HashSet<string> { "a", "b" },
            new HashSet<string> { "a", "b" }));
    }

    [Fact]
    public void DictionariesWithEqualEntries_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new Dictionary<string, string> { ["a"] = "1" },
            new Dictionary<string, string> { ["a"] = "1" }));
    }

    [Fact]
    public void CollectionWrappersWithEqualElements_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new Collection<string> { "a" },
            new Collection<string> { "a" }));
        Assert.False(InitializerValueEquality.ValuesEqual(
            new ReadOnlyCollection<string>(["a"]),
            new ReadOnlyCollection<string>(["a"])));
        Assert.False(InitializerValueEquality.ValuesEqual(
            new ArrayList { "a" },
            new ArrayList { "a" }));
    }

    [Fact]
    public void CustomListImplementationsWithEqualElements_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new CustomStringList("a"),
            new CustomStringList("a")));
        Assert.False(InitializerValueEquality.ValuesEqual(
            new StringListSubclass("a"),
            new StringListSubclass("a")));
    }

    [Fact]
    public void ExactMaterializedListAndListSubclass_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            new List<string> { "a" },
            new StringListSubclass("a")));
        Assert.False(InitializerValueEquality.ValuesEqual(
            new StringListSubclass("a"),
            new List<string> { "a" }));
    }

    [Fact]
    public void ImmutableListsWithEqualElements_CompareUnequal()
    {
        Assert.False(InitializerValueEquality.ValuesEqual(
            ImmutableList.Create("a", "b"),
            ImmutableList.Create("a", "b")));
    }

    [Fact]
    public void SameOpaqueCollectionReference_ComparesUnequal()
    {
        var hashSet = new HashSet<string> { "a" };
        var collection = new Collection<string> { "a" };
        var listSubclass = new StringListSubclass("a");
        var customList = new CustomStringList("a");

        Assert.False(InitializerValueEquality.ValuesEqual(hashSet, hashSet));
        Assert.False(InitializerValueEquality.ValuesEqual(collection, collection));
        Assert.False(InitializerValueEquality.ValuesEqual(listSubclass, listSubclass));
        Assert.False(InitializerValueEquality.ValuesEqual(customList, customList));
    }

    [Fact]
    public void LazyEnumerables_CompareUnequalWithoutEnumeration()
    {
        var first = new CountingEnumerable<string>(YieldTags());
        var second = new CountingEnumerable<string>(YieldTags());

        Assert.False(InitializerValueEquality.ValuesEqual(first, second));
        Assert.False(InitializerValueEquality.ValuesEqual(new List<string> { "a", "b" }, first));
        Assert.Equal(0, first.EnumerationCount);
        Assert.Equal(0, second.EnumerationCount);
    }

    [Fact]
    public void OneShotEnumerables_CompareUnequalWithoutEnumeration()
    {
        var first = new OneShotEnumerable(["a", "b"]);
        var second = new OneShotEnumerable(["a", "b"]);

        Assert.False(InitializerValueEquality.ValuesEqual(first, second));
        Assert.False(first.WasRequested);
        Assert.False(second.WasRequested);
    }

    private static IEnumerable<string> YieldTags()
    {
        yield return "a";
        yield return "b";
    }

    private sealed class CountingEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _inner;

        public CountingEnumerable(IEnumerable<T> inner) => _inner = inner;

        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class OneShotEnumerable : IEnumerable<string>
    {
        private readonly IEnumerator<string> _enumerator;

        public OneShotEnumerable(IEnumerable<string> values) => _enumerator = values.GetEnumerator();

        public bool WasRequested { get; private set; }

        public IEnumerator<string> GetEnumerator()
        {
            if (WasRequested)
                throw new InvalidOperationException("The enumerable was already requested.");

            WasRequested = true;
            return _enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed record CustomScalar(int Value);

    private sealed class StringListSubclass : List<string>
    {
        public StringListSubclass(params string[] items) : base(items)
        {
        }
    }

    private sealed class CustomStringList : IList<string>
    {
        private readonly List<string> _items;

        public CustomStringList(params string[] items) => _items = [.. items];

        public string this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(string item) => _items.Add(item);

        public void Clear() => _items.Clear();

        public bool Contains(string item) => _items.Contains(item);

        public void CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(string item) => _items.IndexOf(item);

        public void Insert(int index, string item) => _items.Insert(index, item);

        public bool Remove(string item) => _items.Remove(item);

        public void RemoveAt(int index) => _items.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
