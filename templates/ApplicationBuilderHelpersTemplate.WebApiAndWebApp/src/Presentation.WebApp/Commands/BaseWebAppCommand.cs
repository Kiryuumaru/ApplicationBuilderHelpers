using ApplicationBuilderHelpers;
using Microsoft.Extensions.Hosting;
using Presentation.Commands;
using System.Diagnostics.CodeAnalysis;

namespace Presentation.WebApp.Commands;

public abstract class BaseWebAppCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : BaseCommand<THostApplicationBuilder>
    where THostApplicationBuilder : IHostApplicationBuilder
{

}
