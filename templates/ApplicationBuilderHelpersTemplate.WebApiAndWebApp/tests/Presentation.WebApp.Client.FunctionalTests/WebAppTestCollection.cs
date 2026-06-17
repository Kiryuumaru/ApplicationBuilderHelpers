using Presentation.WebApp.Client.FunctionalTests.Fixtures;

namespace Presentation.WebApp.Client.FunctionalTests;

/// <summary>
/// Test collection for WebApp functional tests.
/// Shares ONLY the Playwright browser instance (expensive to start).
/// Each test creates its own WebApiTestHost (unique port + unique in-memory DB).
/// This ensures complete test isolation while keeping browser startup cost low.
/// </summary>
[CollectionDefinition(Name)]
public class WebAppTestCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "WebApp Functional Tests";
}
