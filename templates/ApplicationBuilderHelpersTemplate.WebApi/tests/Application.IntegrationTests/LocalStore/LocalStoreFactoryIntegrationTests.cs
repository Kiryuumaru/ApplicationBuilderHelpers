using Application.LocalStore.Extensions;
using Application.LocalStore.Interfaces.Inbound;
using Infrastructure.EFCore.LocalStore.Extensions;
using Infrastructure.EFCore.Sqlite.Extensions;
using Infrastructure.EFCore.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Application.IntegrationTests.LocalStore;

/// <summary>
/// Integration tests for ILocalStoreFactory behavior.
/// Uses DI to obtain the real implementation - tests remain persistence-ignorant.
/// </summary>
public sealed class LocalStoreFactoryIntegrationTests
{
	[Fact]
	public async Task SetAndGet_PersistsDataAfterCommit()
	{
		using var fixture = await CreateFixture();

		using (var store = await fixture.Factory.OpenStoreAsync("orders", CancellationToken.None))
		{
			await store.SetAsync("42", "payload", CancellationToken.None);
			await store.CommitAsync(CancellationToken.None);
		}

		using (var verifier = await fixture.Factory.OpenStoreAsync("orders", CancellationToken.None))
		{
			var value = await verifier.GetAsync("42", CancellationToken.None);
			Assert.Equal("payload", value);
			await verifier.RollbackAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task RollbackAsync_RemovesUncommittedChanges()
	{
		using var fixture = await CreateFixture();

		using (var store = await fixture.Factory.OpenStoreAsync("orders", CancellationToken.None))
		{
			await store.SetAsync("temp", "value", CancellationToken.None);
			await store.RollbackAsync(CancellationToken.None);
		}

		using (var verifier = await fixture.Factory.OpenStoreAsync("orders", CancellationToken.None))
		{
			var value = await verifier.GetAsync("temp", CancellationToken.None);
			Assert.Null(value);
			await verifier.RollbackAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task ConcurrentLocalStore_CommitsAllParallelWrites()
	{
		using var fixture = await CreateFixture();
		const int itemCount = 25;

		using (var store = await fixture.Factory.OpenStoreAsync("bulk", CancellationToken.None))
		{
			var tasks = Enumerable.Range(0, itemCount)
				.Select(i => Task.Run(async () =>
				{
					await store.SetAsync($"item-{i}", $"value-{i}", CancellationToken.None);
				}));

			await Task.WhenAll(tasks);
			await store.CommitAsync(CancellationToken.None);
		}

		using (var verifier = await fixture.Factory.OpenStoreAsync("bulk", CancellationToken.None))
		{
			for (var i = 0; i < itemCount; i++)
			{
				var value = await verifier.GetAsync($"item-{i}", CancellationToken.None);
				Assert.Equal($"value-{i}", value);
			}
			await verifier.RollbackAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task Contains_ReflectsCommittedInsertAndDelete()
	{
		using var fixture = await CreateFixture();

		using (var writer = await fixture.Factory.OpenStoreAsync("orders", CancellationToken.None))
		{
			Assert.False(await writer.ContainsAsync("alpha", CancellationToken.None));
			await writer.SetAsync(" alpha ", "first", CancellationToken.None);
			await writer.CommitAsync(CancellationToken.None);
		}

		using (var reader = await fixture.Factory.OpenStoreAsync("orders", CancellationToken.None))
		{
			Assert.True(await reader.ContainsAsync("alpha", CancellationToken.None));
			await reader.DeleteAsync("alpha", CancellationToken.None);
			await reader.CommitAsync(CancellationToken.None);
		}

		using (var verifier = await fixture.Factory.OpenStoreAsync(" orders ", CancellationToken.None))
		{
			Assert.False(await verifier.ContainsAsync(" alpha ", CancellationToken.None));
			await verifier.RollbackAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task GetIds_ReturnsNormalizedIdentifiersForSpecifiedGroup()
	{
		using var fixture = await CreateFixture();

		using (var writer = await fixture.Factory.OpenStoreAsync(" portfolio ", CancellationToken.None))
		{
			await writer.SetAsync("  ID-1  ", "value1", CancellationToken.None);
			await writer.SetAsync("ID-2", "value2", CancellationToken.None);
			await writer.CommitAsync(CancellationToken.None);
		}

		// Write to different group to ensure isolation
		using (var otherWriter = await fixture.Factory.OpenStoreAsync("other", CancellationToken.None))
		{
			await otherWriter.SetAsync("ignored", "value3", CancellationToken.None);
			await otherWriter.CommitAsync(CancellationToken.None);
		}

		using (var verifier = await fixture.Factory.OpenStoreAsync("  portfolio  ", CancellationToken.None))
		{
			var ids = await verifier.GetIdsAsync(CancellationToken.None);
			Assert.Equal(new[] { "ID-1", "ID-2" }, ids);
			await verifier.RollbackAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task LocalStoreScenario_SerializesAndDeletesVariousTypes()
	{
		using var fixture = await CreateFixture();
		var payloadTypeInfo = GetTypeInfo<SamplePayload>();

		var decimalValue = 42.42m;
		var timestamp = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
		var timestampOffset = new DateTimeOffset(2024, 2, 3, 4, 5, 6, TimeSpan.FromHours(-3));
		var duration = TimeSpan.FromMinutes(12.5);

		using (var store = await fixture.Factory.OpenStoreAsync("common_group", CancellationToken.None))
		{
			var payload = new SamplePayload
			{
				Name = "Widget",
				Quantity = 3,
				Tags = ["alpha", "beta"]
			};

			await store.SetAsync("TestKey", "TestValue", CancellationToken.None);
			await store.SetAsync("Sample:Payload", payload, payloadTypeInfo, CancellationToken.None);
			await store.SetAsync("Sample:Number", decimalValue, CancellationToken.None);
			await store.SetAsync("Sample:Flag", true, CancellationToken.None);
			await store.SetAsync("Sample:Timestamp", timestamp, CancellationToken.None);
			await store.SetAsync("Sample:TimestampOffset", timestampOffset, CancellationToken.None);
			await store.SetAsync("Sample:Duration", duration, CancellationToken.None);
			await store.SetAsync("Sample:Array", JsonSerializer.Serialize(new[] { "one", "two", "three" }, SerializerOptions), CancellationToken.None);
			await store.SetAsync("Sample:Object", JsonSerializer.Serialize(new Dictionary<string, object?>
			{
				["Nested"] = new { Enabled = true, Count = 4 },
				["Timestamp"] = DateTimeOffset.UtcNow
			}, SerializerOptions), CancellationToken.None);

			var keys = await store.GetIdsAsync(CancellationToken.None);
			Assert.Contains("Sample:Payload", keys);
			Assert.Contains("Sample:Array", keys);
			Assert.Contains("Sample:Number", keys);

			Assert.True(await store.ContainsAsync("Sample:Array", CancellationToken.None));
			await store.DeleteAsync("Sample:Array", CancellationToken.None);
			Assert.False(await store.ContainsAsync("Sample:Array", CancellationToken.None));

			await store.CommitAsync(CancellationToken.None);
		}

		using (var store = await fixture.Factory.OpenStoreAsync("common_group", CancellationToken.None))
		{
			var restored = await store.GetAsync("Sample:Payload", payloadTypeInfo, CancellationToken.None);
			Assert.NotNull(restored);
			Assert.Equal("Widget", restored!.Name);
			Assert.Equal(3, restored.Quantity);
			Assert.Equal(["alpha", "beta"], restored.Tags);

			var number = await store.GetAsync<decimal>("Sample:Number", CancellationToken.None);
			Assert.Equal(decimalValue, number);

			var flag = await store.GetAsync<bool>("Sample:Flag", CancellationToken.None);
			Assert.True(flag);

			// DateTime may be converted to local time during parsing, compare as UTC
			var restoredTimestamp = await store.GetAsync<DateTime>("Sample:Timestamp", CancellationToken.None);
			Assert.Equal(timestamp.ToUniversalTime(), restoredTimestamp.ToUniversalTime());

			var restoredTimestampOffset = await store.GetAsync<DateTimeOffset>("Sample:TimestampOffset", CancellationToken.None);
			Assert.Equal(timestampOffset, restoredTimestampOffset);

			var restoredDuration = await store.GetAsync<TimeSpan>("Sample:Duration", CancellationToken.None);
			Assert.Equal(duration, restoredDuration);

			var arrayJson = await store.GetAsync("Sample:Array", CancellationToken.None);
			Assert.Null(arrayJson);

			var ids = await store.GetIdsAsync(CancellationToken.None);
			Assert.DoesNotContain("Sample:Array", ids);
			Assert.Contains("Sample:Object", ids);
			Assert.Contains("Sample:Timestamp", ids);

			await store.RollbackAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task Dispose_AutoCommitsUncommittedChanges()
	{
		using var fixture = await CreateFixture();

		// Set a value and dispose without calling CommitAsync
		using (var store = await fixture.Factory.OpenStoreAsync("transactions", CancellationToken.None))
		{
			await store.SetAsync("item-1", "auto-committed-value", CancellationToken.None);
			// No explicit CommitAsync - just dispose
		}

		// Verify the data persisted after auto-commit on dispose
		using (var verifier = await fixture.Factory.OpenStoreAsync("transactions", CancellationToken.None))
		{
			var value = await verifier.GetAsync("item-1", CancellationToken.None);
			Assert.Equal("auto-committed-value", value);
			await verifier.RollbackAsync(CancellationToken.None);
		}
	}

	[Fact]
	public async Task ConcurrentLocalStore_Dispose_AutoCommitsUncommittedChanges()
	{
		using var fixture = await CreateFixture();

		// Set values through store and dispose without calling CommitAsync
		using (var store = await fixture.Factory.OpenStoreAsync("concurrent_group", CancellationToken.None))
		{
			await store.SetAsync("key-1", "value-1", CancellationToken.None);
			await store.SetAsync("key-2", "value-2", CancellationToken.None);
			// No explicit CommitAsync - just dispose
		}

		// Verify the data persisted
		using (var verifier = await fixture.Factory.OpenStoreAsync("concurrent_group", CancellationToken.None))
		{
			var value1 = await verifier.GetAsync("key-1", CancellationToken.None);
			var value2 = await verifier.GetAsync("key-2", CancellationToken.None);

			Assert.Equal("value-1", value1);
			Assert.Equal("value-2", value2);

			await verifier.RollbackAsync(CancellationToken.None);
		}
	}

	private static async Task<TestFixture> CreateFixture()
	{
		// Use in-memory SQLite database for test isolation
		var dbName = Guid.NewGuid().ToString();
		var connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

		// Build services via DI - persistence ignorant pattern
		var services = new ServiceCollection();
		services.AddLogging();
		
		// Register EFCore infrastructure (includes EFCoreDatabaseInitializationState)
		services.AddEFCoreInfrastructure();
		services.AddEFCoreSqlite(connectionString);
		services.AddEFCoreLocalStore();
		
		// Register Application layer services
		services.AddLocalStoreServices();

		var provider = services.BuildServiceProvider();
		
		// Keep a connection open to preserve the in-memory database and initialize schema
		var dbContextFactory = provider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Infrastructure.EFCore.EFCoreDbContext>>();
		var dbContext = dbContextFactory.CreateDbContext();
		await dbContext.Database.EnsureCreatedAsync();
		
		// Signal that database is initialized (uses internal method via InternalsVisibleTo)
		var initState = provider.GetRequiredService<Infrastructure.EFCore.Services.EFCoreDatabaseInitializationState>();
		initState.MarkInitialized();
		
		var factory = provider.GetRequiredService<ILocalStoreFactory>();

		return new TestFixture(factory, provider, dbContext);
	}

	private static JsonTypeInfo<T?> GetTypeInfo<T>() where T : class
		=> (JsonTypeInfo<T?>)SerializerOptions.GetTypeInfo(typeof(T))!;

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		TypeInfoResolver = new DefaultJsonTypeInfoResolver()
	};

	private sealed class SamplePayload
	{
		public string Name { get; set; } = string.Empty;
		public int Quantity { get; set; }
		public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
	}

	private sealed record TestFixture(
		ILocalStoreFactory Factory,
		ServiceProvider Provider,
		IDisposable DbContext) : IDisposable
	{
		public void Dispose()
		{
			Provider.Dispose();
			DbContext.Dispose();
		}
	}
}
