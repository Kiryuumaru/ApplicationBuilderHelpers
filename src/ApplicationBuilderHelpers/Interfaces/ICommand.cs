using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Caller-view command contract; prefer deriving <c>Command&lt;T&gt;</c> over implementing directly.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommand : IApplicationDependency
{
    internal void CommandPreparationInternal(ApplicationBuilder applicationBuilder);

    internal ValueTask<ApplicationHostBuilder> ApplicationBuilderInternal(CancellationToken stoppingToken);

    internal ValueTask RunInternal(ApplicationHost applicationHost, CancellationToken cancellationToken);
}
