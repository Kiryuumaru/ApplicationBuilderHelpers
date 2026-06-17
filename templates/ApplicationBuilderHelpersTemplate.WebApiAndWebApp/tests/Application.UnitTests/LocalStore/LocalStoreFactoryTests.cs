using Application.LocalStore.Interfaces.Outbound;
using Application.LocalStore.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UnitTests.LocalStore;

public class LocalStoreFactoryTests
{
	[Fact]
	public async Task OpenStore_NormalizesGroupAndInvokesServiceOpen()
	{
		var service = Substitute.For<ILocalStoreService>();
		using var provider = new ServiceCollection()
			.AddSingleton(service)
			.BuildServiceProvider();
		var factory = new LocalStoreFactory(provider);
		using var cts = new CancellationTokenSource();

		using var store = await factory.OpenStore(" Demo ", cts.Token);

		await service.Received(1).Open(cts.Token);
		Assert.Equal("Demo", store.Group);
	}

	[Fact]
	public async Task OpenStore_ThrowsForInvalidGroup()
	{
		var service = Substitute.For<ILocalStoreService>();
		using var provider = new ServiceCollection()
			.AddSingleton(service)
			.BuildServiceProvider();
		var factory = new LocalStoreFactory(provider);

		await Assert.ThrowsAsync<ArgumentException>(() => factory.OpenStore("  "));
	}
}
