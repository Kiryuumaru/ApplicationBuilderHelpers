using Microsoft.Extensions.Logging;

namespace ApplicationBuilderHelpers.Test.Cli.Services;

internal class MockService(ILogger<MockService> logger)
{
    private readonly ILogger<MockService> _logger = logger;

    public void Print()
    {
        _logger.LogInformation("Printed from MockService");
    }
}
