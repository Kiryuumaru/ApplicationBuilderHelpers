using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process tests for the C9 reuse-only service-property seam in
/// <see cref="CommandExecutor"/>: per-command scope injection of
/// <c>FromServices</c>/<c>FromKeyedServices</c>-marked properties, disjoint
/// from CLI binding, with faults mapping to exit 1 and help/version/
/// validation paths never reaching the executor.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
/// <remarks>
/// Reuse-only seam: no new attribute types are introduced. The framework
/// service attributes cannot be applied to properties in C# source (the
/// upstream <c>FromKeyedServicesAttribute</c> targets parameters only, and
/// the ASP.NET Core <c>FromServicesAttribute</c> would add a package
/// dependency), so tests use same-named local shims that the injector
/// matches by attribute name and reads via <c>CustomAttributeData</c>.
/// </remarks>
[Collection("ConsoleDecoupling")]
public sealed class ServicePropertyInjectionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    private sealed class FromServicesAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    private sealed class FromKeyedServicesAttribute(object key) : Attribute
    {
        public object Key { get; } = key;
    }

    public sealed class ProbeService
    {
        public Guid InstanceId { get; } = Guid.NewGuid();
    }

    public sealed class ScopedProbe : IDisposable
    {
        public Guid InstanceId { get; } = Guid.NewGuid();
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    [Command("svcscoped", "Probes scoped service injection.")]
    public sealed class ScopedInjectCommand : Command
    {
        public static readonly List<Guid> SeenScopedIds = [];
        public static ScopedProbe? LastScoped;

        [FromServices]
        public ProbeService? Probe { get; set; }

        [FromServices]
        public ScopedProbe? Scoped { get; set; }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddSingleton<ProbeService>();
            services.AddScoped<ScopedProbe>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            SeenScopedIds.Add(Scoped!.InstanceId);
            LastScoped = Scoped;
            Console.WriteLine($"probe:{Probe!.InstanceId}");
            Console.WriteLine($"scoped:{Scoped.InstanceId}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("svckeyed", "Probes keyed service injection from the same scope.")]
    public sealed class KeyedInjectCommand : Command
    {
        [FromServices]
        public ScopedProbe? Unkeyed { get; set; }

        [FromKeyedServices("primary")]
        public ScopedProbe? Keyed { get; set; }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddScoped<ScopedProbe>();
            services.AddKeyedScoped<ScopedProbe>("primary", (sp, _) => sp.GetRequiredService<ScopedProbe>());
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"same-scope:{ReferenceEquals(Unkeyed, Keyed)}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("svcbind", "Probes CLI values surviving service injection.")]
    public sealed class BindPreserveCommand : Command
    {
        [CommandOption("name", Description = "Name value.")]
        public string? Name { get; set; }

        [FromServices]
        public ProbeService? Probe { get; set; }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddSingleton<ProbeService>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"name:{Name}");
            Console.WriteLine($"probe-set:{Probe is not null}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("svcdual", "Probes dual-marked property rejection.")]
    public sealed class DualMarkedCommand : Command
    {
        [CommandOption("name", Description = "Name value.")]
        [FromServices]
        public ProbeService? Dual { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("dual ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("svcmissing", "Probes missing service mapping.")]
    public sealed class MissingServiceCommand : Command
    {
        [FromServices]
        public ProbeService? Probe { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("missing ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("svchelp", "Probes help path executor reachability.")]
    public sealed class HelpScopeCommand : Command
    {
        public static bool RunInvoked;
        public static bool AddServicesInvoked;

        [FromServices]
        public ProbeService? Probe { get; set; }

        [CommandOption("name", Description = "Name value.")]
        public string? Name { get; set; }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            AddServicesInvoked = true;
            services.AddSingleton<ProbeService>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            RunInvoked = true;
            Console.WriteLine("help-scope ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("svcvalid", "Probes validation path executor reachability.")]
    public sealed class ValidationScopeCommand : Command
    {
        public static bool RunInvoked;
        public static bool AddServicesInvoked;

        [CommandOption("name", Description = "Name value.", Required = true)]
        public string? Name { get; set; }

        [FromServices]
        public ProbeService? Probe { get; set; }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            AddServicesInvoked = true;
            services.AddSingleton<ProbeService>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            RunInvoked = true;
            Console.WriteLine("validation ran");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ScopedInjection_ResolvesFromPerCommandScope()
    {
        ScopedInjectCommand.SeenScopedIds.Clear();
        ScopedInjectCommand.LastScoped = null;

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<ScopedInjectCommand>(), ["svcscoped"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("probe:", output);
        Assert.Contains("scoped:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.NotNull(ScopedInjectCommand.LastScoped);
        Assert.Single(ScopedInjectCommand.SeenScopedIds);
    }

    [Fact]
    public async Task ScopedInjection_IsIsolatedPerCommandRun()
    {
        ScopedInjectCommand.SeenScopedIds.Clear();
        ScopedInjectCommand.LastScoped = null;

        var first = await RunCapturedAsync(() => CreateBuilder<ScopedInjectCommand>(), ["svcscoped"]);
        var second = await RunCapturedAsync(() => CreateBuilder<ScopedInjectCommand>(), ["svcscoped"]);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(2, ScopedInjectCommand.SeenScopedIds.Count);
        Assert.NotEqual(ScopedInjectCommand.SeenScopedIds[0], ScopedInjectCommand.SeenScopedIds[1]);
    }

    [Fact]
    public async Task ScopedInjection_DisposesScopeAfterRun()
    {
        ScopedInjectCommand.SeenScopedIds.Clear();
        ScopedInjectCommand.LastScoped = null;

        var (exitCode, _, _) = await RunCapturedAsync(
            () => CreateBuilder<ScopedInjectCommand>(), ["svcscoped"]);

        Assert.Equal(0, exitCode);
        Assert.NotNull(ScopedInjectCommand.LastScoped);
        Assert.True(ScopedInjectCommand.LastScoped.Disposed);
    }

    [Fact]
    public async Task InjectAfterBind_PreservesCliValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<BindPreserveCommand>(), ["svcbind", "--name", "cli-value"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("name:cli-value", output);
        Assert.Contains("probe-set:True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DualMarkedProperty_MapsToFaultNeverUsage()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<DualMarkedCommand>(), ["svcdual"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("either CLI-bound or service-injected", error);
    }

    [Fact]
    public async Task KeyedInjection_ResolvesFromSameScope()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<KeyedInjectCommand>(), ["svckeyed"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("same-scope:True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MissingService_MapsToFaultNeverUsage()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<MissingServiceCommand>(), ["svcmissing"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error:", error);
    }

    [Fact]
    public async Task HelpPath_CreatesZeroScopes()
    {
        HelpScopeCommand.RunInvoked = false;
        HelpScopeCommand.AddServicesInvoked = false;

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HelpScopeCommand>(), ["svchelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.False(HelpScopeCommand.RunInvoked);
        Assert.False(HelpScopeCommand.AddServicesInvoked);
        Assert.DoesNotContain("help-scope ran", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task VersionPath_CreatesZeroScopes()
    {
        HelpScopeCommand.RunInvoked = false;
        HelpScopeCommand.AddServicesInvoked = false;

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HelpScopeCommand>(), ["--version"]);

        Assert.Equal(0, exitCode);
        Assert.False(HelpScopeCommand.RunInvoked);
        Assert.False(HelpScopeCommand.AddServicesInvoked);
        Assert.DoesNotContain("help-scope ran", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ValidationPath_CreatesZeroScopes()
    {
        ValidationScopeCommand.RunInvoked = false;
        ValidationScopeCommand.AddServicesInvoked = false;

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<ValidationScopeCommand>(), ["svcvalid"]);

        Assert.Equal(2, exitCode);
        Assert.False(ValidationScopeCommand.RunInvoked);
        Assert.False(ValidationScopeCommand.AddServicesInvoked);
        Assert.DoesNotContain("validation ran", output);
        Assert.Contains("Missing required option", error);
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("svc-test")
            .SetExecutableTitle("Svc Test")
            .SetExecutableDescription("Service injection verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<TCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args, CancellationToken cancellationToken = default)
    {
        await ConsoleGate.WaitAsync();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await builderFactory().RunAsync(args, cancellationToken);
            outWriter.Flush();
            errorWriter.Flush();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }
}
