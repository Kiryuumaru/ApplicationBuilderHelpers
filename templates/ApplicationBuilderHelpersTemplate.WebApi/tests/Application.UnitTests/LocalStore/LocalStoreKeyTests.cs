using Application.LocalStore.Utilities;

namespace Application.UnitTests.LocalStore;

public sealed class LocalStoreKeyTests
{
	[Fact]
	public void NormalizeGroup_TrimsInput()
	{
		var result = LocalStoreKey.NormalizeGroup("  demo-group  ");
		Assert.Equal("demo-group", result);
	}

	[Fact]
	public void NormalizeId_ThrowsForEmpty()
	{
		Assert.Throws<ArgumentException>(() => LocalStoreKey.NormalizeId("  "));
	}

	[Fact]
	public void BuildStorageKey_RoundTrips()
	{
		var group = "alpha";
		var id = "beta";
		var storage = LocalStoreKey.BuildStorageKey(group, id);
		Assert.True(LocalStoreKey.TryExtractIdFromStorageKey(storage, group, out var extracted));
		Assert.Equal(id, extracted);
	}
}
