using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal class CommandLineParser(ApplicationBuilder builder)
{
    internal async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
