namespace Presentation.WebApp.Server.FunctionalTests.Fixtures;

/// <summary>
/// Configuration for test credentials.
/// Reads from Environment Variables.
/// </summary>
public sealed class TestCredentials
{
    public string? BinanceApiKey { get; private set; }
    public string? BinanceApiSecret { get; private set; }
    public bool HasBinanceCredentials => !string.IsNullOrEmpty(BinanceApiKey) && !string.IsNullOrEmpty(BinanceApiSecret);

    public static TestCredentials Load()
    {
        var credentials = new TestCredentials
        {
            // Read from environment variables
            BinanceApiKey = Environment.GetEnvironmentVariable("BINANCE_TEST_API_KEY"),
            BinanceApiSecret = Environment.GetEnvironmentVariable("BINANCE_TEST_API_SECRET")
        };

        return credentials;
    }
}
