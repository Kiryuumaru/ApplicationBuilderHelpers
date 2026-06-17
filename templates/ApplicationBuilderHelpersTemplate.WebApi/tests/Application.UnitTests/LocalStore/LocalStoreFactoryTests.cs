using Application.LocalStore.Interfaces.Outbound;
using Application.LocalStore.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Application.UnitTests.LocalStore;

public sealed class LocalStoreFactoryTests
{
	[Fact]
	public async Task OpenStoreAsync_NormalizesGroupAndInvokesServiceOpen()
	{
		var service = Substitute.For<ILocalStoreService>();
		using var provider = new ServiceCollection()
			.AddSingleton(service)
			.BuildServiceProvider();
		var factory = new LocalStoreFactory(provider);
		using var cts = new CancellationTokenSource();

		using var store = await factory.OpenStoreAsync(" Demo ", cts.Token);

		await service.Received(1).OpenAsync(cts.Token);
		Assert.Equal("Demo", store.Group);
	}

	[Fact]
	public async Task OpenStoreAsync_ThrowsForInvalidGroup()
	{
		var service = Substitute.For<ILocalStoreService>();
		using var provider = new ServiceCollection()
			.AddSingleton(service)
			.BuildServiceProvider();
		var factory = new LocalStoreFactory(provider);

		await Assert.ThrowsAsync<ArgumentException>(() => factory.OpenStoreAsync("  "));
	}
}
