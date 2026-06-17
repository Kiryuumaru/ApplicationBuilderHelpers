# Test Architecture

## WebApi Functional Tests

Since WebApi uses `ApplicationBuilderHelpers.ApplicationBuilder.Create()` pattern instead of standard ASP.NET Core startup, we can't use `WebApplicationFactory<Program>`. Instead, we run WebApi as a subprocess.

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Test Process (xUnit)                                                   │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  SharedWebApiHost (ICollectionFixture)                            │ │
│  │  - Starts WebApi.exe subprocess on port 5199                      │ │
│  │  - Shared by all tests via [Collection("WebApi Tests")]           │ │
│  │  - Disables parallel execution to avoid port conflicts            │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                      │                                  │
│                                      │ HTTP                             │
│                                      ▼                                  │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  WebApiTestHost                                                   │ │
│  │  - Runs: src/Presentation.WebApp/bin/Debug/net10.0/WebApi.exe    │ │
│  │  - Uses ASPNETCORE_URLS env var for URL configuration            │ │
│  │  - Waits for server ready via polling                             │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

## Key Files

| File | Purpose |
|------|---------|
| `WebApiTestCollection.cs` | Defines `SharedWebApiHost` fixture + collection |
| `Fixtures/WebApiTestHost.cs` | Subprocess host implementation |
| `Fixtures/WebApiTestHelpers.cs` | Helper methods for test setup |

## Test Guidelines

### Authentication
- All authenticated endpoints require valid JWT in `Authorization: Bearer {token}` header
- Use `WebApiTestHelpers.RegisterAndLoginAsync()` to get test tokens

### Real Infrastructure
- All tests use **real infrastructure**
- No mocks - real infrastructure only
- Fresh SQLite database per test run

### File Organization
Each controller has its own test file:
```
tests/Presentation.WebApp.FunctionalTests/
├── Auth/
│   └── AuthApiTests.cs
├── Users/
│   └── UsersApiTests.cs
└── Bootstrap/
    └── BootstrapApiTests.cs
```

## Writing New Tests

### Test Class Structure

```csharp
[Collection(WebApiTestCollection.Name)]
public class MyFeatureApiTests
{
    private readonly ITestOutputHelper _output;
    private readonly SharedWebApiHost _sharedHost;
    
    public MyFeatureApiTests(SharedWebApiHost sharedHost, ITestOutputHelper output)
    {
        _sharedHost = sharedHost;
        _output = output;
    }

    [Fact]
    public async Task MyEndpoint_WithValidInput_ReturnsExpectedResult()
    {
        _output.WriteLine("[TEST] MyEndpoint_WithValidInput_ReturnsExpectedResult");
        
        // Arrange
        _output.WriteLine("[STEP] Register and login test user...");
        var (token, userId) = await RegisterAndLoginAsync();
        
        // Act
        _output.WriteLine("[STEP] Call endpoint...");
        var response = await _sharedHost.Host.HttpClient.GetAsync("/api/v1/my-endpoint");
        
        _output.WriteLine($"[RECEIVED] Status: {(int)response.StatusCode}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _output.WriteLine("[PASS] Test completed successfully");
    }
}
```

### Naming Convention

`Action_Condition_ExpectedResult`

Examples:
- `Login_WithValidCredentials_ReturnsJwtToken`
- `CreateAccount_WithDuplicateName_Returns409`
- `GetOrder_OtherUsersOrder_Returns403`

### Logging Convention

- `[TEST]` - Test name at start
- `[STEP]` - Each significant action
- `[RECEIVED]` - Response from API
- `[PASS]` - Success confirmation

## Running Tests

```bash
# Run all functional tests
dotnet test tests/Presentation.WebApp.FunctionalTests

# Run specific test class
dotnet test tests/Presentation.WebApp.FunctionalTests --filter "FullyQualifiedName~AuthApiTests"

# Run specific test
dotnet test tests/Presentation.WebApp.FunctionalTests --filter "FullyQualifiedName~Login_WithValidCredentials"
```

## Pre-Test Build

Always rebuild WebApi before running tests:

```bash
dotnet build src/Presentation.WebApp
dotnet test tests/Presentation.WebApp.FunctionalTests
```

## Test Summary

| Project | Tests |
|---------|-------|
| Domain.UnitTests | 46 |
| Application.UnitTests | 67 |
| Application.IntegrationTests | 20 |
| Presentation.WebApp.FunctionalTests | 378 |
| **Total** | **511** |
