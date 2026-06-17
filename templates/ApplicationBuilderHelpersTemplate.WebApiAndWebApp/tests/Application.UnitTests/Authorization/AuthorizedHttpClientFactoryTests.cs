using Application.Server.Authorization.Interfaces.Inbound;
using Application.Server.Authorization.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;

namespace Application.UnitTests.Authorization;

public class AuthorizedHttpClientFactoryTests
{
	[Fact]
	public async Task CreateAuthorized_SetsBearerTokenAndTracksDisposal()
	{
		var permissions = new[] { "perm.one", "perm.two" };
		var expiration = TimeSpan.FromMinutes(30);
		var permissionService = Substitute.For<IPermissionService>();
		DateTimeOffset? capturedExpiration = null;

		permissionService
			.GenerateApiKeyTokenWithPermissionsAsync(
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>>(),
				Arg.Any<IEnumerable<Claim>>(),
				Arg.Any<DateTimeOffset?>(),
				Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedExpiration = callInfo.ArgAt<DateTimeOffset?>(3);
				return Task.FromResult("api-token-value");
			});

		var handler = new TrackingHandler();
		var httpClient = new HttpClient(handler, disposeHandler: true);

		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		httpClientFactory.CreateClient("authorization-service").Returns(httpClient);

		var factory = new AuthorizedHttpClientFactory(
			serviceKey: "authorization-service",
			logger: NullLogger<AuthorizedHttpClientFactory>.Instance,
			httpClientFactory: httpClientFactory,
			permissionService: permissionService);

		var client = await factory.CreateAuthorizedAsync("client-A", permissions, expiration, CancellationToken.None);

		Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization?.Scheme);
		Assert.Equal("api-token-value", client.DefaultRequestHeaders.Authorization?.Parameter);
		Assert.Equal(expiration, client.Timeout);

		await permissionService.Received(1).GenerateApiKeyTokenWithPermissionsAsync(
			"client-A",
			Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(permissions)),
			Arg.Is<IEnumerable<Claim>?>(claims => claims == null),
			Arg.Any<DateTimeOffset?>(),
			Arg.Any<CancellationToken>());

		Assert.NotNull(capturedExpiration);
		var expectedExpiration = DateTimeOffset.UtcNow.Add(expiration);
		Assert.InRange((capturedExpiration!.Value - expectedExpiration).Duration(), TimeSpan.Zero, TimeSpan.FromSeconds(5));

		factory.Dispose();
		Assert.True(handler.IsDisposed);
	}

	private sealed class TrackingHandler : HttpMessageHandler
	{
		public bool IsDisposed { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			IsDisposed = true;
		}
	}
}
