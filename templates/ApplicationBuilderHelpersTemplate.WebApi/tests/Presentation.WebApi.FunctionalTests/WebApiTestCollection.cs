namespace Presentation.WebApi.FunctionalTests;

/// <summary>
/// Test collection for WebApi functional tests.
/// Tests run in parallel - each test class manages its own host with random port and database.
/// </summary>
[CollectionDefinition(Name)]
public class WebApiTestCollection
{
    public const string Name = "WebApi Tests";
}
