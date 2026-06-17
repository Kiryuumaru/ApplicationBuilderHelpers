using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Represents a command that can be executed within the application.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommand : IApplicationDependency
{
    internal void CommandPreparationInternal(ApplicationBuilder applicationBuilder);

    internal ValueTask<ApplicationHostBuilder> ApplicationBuilderInternal(CancellationToken stoppingToken);

    internal ValueTask RunInternal(ApplicationHost applicationHost, CancellationTokenSource cancellationTokenSource);
}
