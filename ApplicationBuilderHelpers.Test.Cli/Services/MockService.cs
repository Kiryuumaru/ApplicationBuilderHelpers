using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Services;

internal class MockService(ILogger<MockService> logger)
{
    private readonly ILogger<MockService> _logger = logger;

    public void Print()
    {
        _logger.LogInformation("Printed from MockService");
    }
}
