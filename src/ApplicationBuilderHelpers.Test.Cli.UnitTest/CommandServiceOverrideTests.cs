using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Covers command-last dependency ordering for duplicate service registrations:
/// the command's own <c>AddServices</c> runs after shared modules, so a plain
/// <c>Add*</c> registration on the command wins the same service (singleton,
/// scoped, transient, and keyed), while <c>TryAdd*</c> keeps its marker meaning
/// on both sides: module-side soft defaults yield, command-side pins do not
/// override, and when both sides use <c>TryAdd*</c> the module wins.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CommandServiceOverrideTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public interface ISharedProbe
    {
        string Origin { get; }
    }

    public sealed class ModuleSharedProbe : ISharedProbe
    {
        public string Origin => "module";
    }

    public sealed class CommandSharedProbe : ISharedProbe
    {
        public string Origin => "command";
    }

    public sealed class PlainSingletonModule : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddSingleton<ISharedProbe, ModuleSharedProbe>();
        }
    }

    public sealed class PlainScopedModule : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddScoped<ISharedProbe, ModuleSharedProbe>();
        }
    }

    public sealed class PlainTransientModule : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddTransient<ISharedProbe, ModuleSharedProbe>();
        }
    }

    public sealed class PlainKeyedSingletonModule : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddKeyedSingleton<ISharedProbe, ModuleSharedProbe>("primary");
        }
    }

    public sealed class SoftDefaultSingletonModule : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.TryAddSingleton<ISharedProbe, ModuleSharedProbe>();
        }
    }

    [Command("singletonprobe", "Resolves a shared singleton registered by both the command and a module.")]
    public sealed class SingletonOverrideCommand : Command
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddSingleton<ISharedProbe, CommandSharedProbe>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var probe = applicationHost.Services.GetRequiredService<ISharedProbe>();
            Console.WriteLine($"singleton:{probe.Origin}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("scopedprobe", "Resolves a shared scoped service registered by both the command and a module.")]
    public sealed class ScopedOverrideCommand : Command
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddScoped<ISharedProbe, CommandSharedProbe>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            using var scope = applicationHost.Services.CreateScope();
            var probe = scope.ServiceProvider.GetRequiredService<ISharedProbe>();
            Console.WriteLine($"scoped:{probe.Origin}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("transientprobe", "Resolves a shared transient service registered by both the command and a module.")]
    public sealed class TransientOverrideCommand : Command
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddTransient<ISharedProbe, CommandSharedProbe>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var probe = applicationHost.Services.GetRequiredService<ISharedProbe>();
            Console.WriteLine($"transient:{probe.Origin}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("keyedprobe", "Resolves a shared keyed singleton registered by both the command and a module.")]
    public sealed class KeyedOverrideCommand : Command
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddKeyedSingleton<ISharedProbe, CommandSharedProbe>("primary");
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var probe = applicationHost.Services.GetRequiredKeyedService<ISharedProbe>("primary");
            Console.WriteLine($"keyed:{probe.Origin}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("softdefaultprobe", "Resolves a shared singleton a module offers as a TryAdd soft default.")]
    public sealed class SoftDefaultYieldCommand : Command
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddSingleton<ISharedProbe, CommandSharedProbe>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var probe = applicationHost.Services.GetRequiredService<ISharedProbe>();
            Console.WriteLine($"softdefault:{probe.Origin}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("commandtryaddprobe", "Resolves a shared singleton a command pins with TryAdd over a module plain add.")]
    public sealed class CommandTryAddCommand : Command
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.TryAddSingleton<ISharedProbe, CommandSharedProbe>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var probe = applicationHost.Services.GetRequiredService<ISharedProbe>();
            Console.WriteLine($"commandtryadd:{probe.Origin}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("bothtryaddprobe", "Resolves a shared singleton both sides register with TryAdd.")]
    public sealed class BothTryAddCommand : Command
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.TryAddSingleton<ISharedProbe, CommandSharedProbe>();
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var probe = applicationHost.Services.GetRequiredService<ISharedProbe>();
            Console.WriteLine($"bothtryadd:{probe.Origin}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task DuplicateSingletonRegistration_ResolvesCommandRegistration()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<SingletonOverrideCommand>().AddApplication<PlainSingletonModule>(), ["singletonprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("singleton:command", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DuplicateScopedRegistration_ResolvesCommandRegistration()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<ScopedOverrideCommand>().AddApplication<PlainScopedModule>(), ["scopedprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("scoped:command", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DuplicateTransientRegistration_ResolvesCommandRegistration()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<TransientOverrideCommand>().AddApplication<PlainTransientModule>(), ["transientprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("transient:command", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DuplicateKeyedSingletonRegistration_ResolvesCommandRegistration()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<KeyedOverrideCommand>().AddApplication<PlainKeyedSingletonModule>(), ["keyedprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("keyed:command", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ModuleSideTryAdd_SoftDefaultYieldsToCommandAdd()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<SoftDefaultYieldCommand>().AddApplication<SoftDefaultSingletonModule>(), ["softdefaultprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("softdefault:command", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CommandSideTryAdd_DoesNotOverrideModuleAdd()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<CommandTryAddCommand>().AddApplication<PlainSingletonModule>(), ["commandtryaddprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("commandtryadd:module", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ModuleAndCommandTryAdd_ModuleRegistrationWins()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<BothTryAddCommand>().AddApplication<SoftDefaultSingletonModule>(), ["bothtryaddprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("bothtryadd:module", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("override-test")
            .SetExecutableTitle("Override Test")
            .SetExecutableDescription("Command service override verification CLI.")
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
