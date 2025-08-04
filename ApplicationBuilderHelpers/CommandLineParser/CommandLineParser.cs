using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal class CommandLineParser(ApplicationBuilder applicationBuilder)
{
    public ApplicationBuilder ApplicationBuilder { get; } = applicationBuilder;

    public ICommandBuilder CommandBuilder { get; } = applicationBuilder;

    public ICommandTypeParserCollection CommandTypeParserCollection { get; } = applicationBuilder;

    public IApplicationDependencyCollection ApplicationDependencyCollection { get; } = applicationBuilder;

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {

    }
}
