using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Executes the target command.
/// </summary>
internal sealed class CommandExecutor(
    IApplicationDependencyCollection applicationDependencyCollection,
    ConsoleOutput consoleOutput)
{
    /// <summary>
    /// Exit code returned when execution is canceled.
    /// Follows the 128 + SIGINT convention.
    /// </summary>
    internal const int CanceledExitCode = 130;

    /// <summary>
    /// Marker for a cancellation request.
    /// Always maps to <see cref="CanceledExitCode"/> (130).
    /// </summary>
    internal sealed class ExternalCancellationException : OperationCanceledException
    {
        public ExternalCancellationException()
            : base("The command was canceled.")
        {
        }
    }

    /// <summary>
    /// Executes the target command. A normal return means success:
    /// the host is stopped after the command returns.
    /// Cancellation token for cooperative cancellation.
    /// </summary>
    public async Task ExecuteCommand(SubCommandInfo commandInfo, CancellationToken cancellationToken)
    {
        var command = commandInfo.Command!;

        using var shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Ctrl+C requests cancellation alongside the passed token.
        bool ctrlCCanceled = false;
        LifetimeGlobalService? lifetimeGlobalService = null;

        ConsoleCancelEventHandler cancelKeyPressHandler = (sender, e) =>
        {
            ctrlCCanceled = true;
            shutdownCts.Cancel();
            e.Cancel = true;
        };

        bool cancelKeyPressSubscribed = false;
        try
        {
            consoleOutput.CancelKeyPress += cancelKeyPressHandler;
            cancelKeyPressSubscribed = true;
        }
        catch (PlatformNotSupportedException)
        {
            consoleOutput.WriteLineError("Note: Console.CancelKeyPress is not supported on this platform.");
        }
        catch (IOException)
        {
            consoleOutput.WriteLineError("Note: Console.CancelKeyPress is unavailable (I/O).");
        }

        try
        {
            var applicationBuilder = await command.ApplicationBuilderInternal(shutdownCts.Token);
            lifetimeGlobalService = new LifetimeGlobalService();
            shutdownCts.Token.Register(lifetimeGlobalService.CancellationTokenSource.Cancel);

            applicationBuilder.Services.AddSingleton(lifetimeGlobalService);
            applicationBuilder.Services.AddScoped<LifetimeService>();

            applicationBuilder.ApplicationDependencies.Add(command);
            foreach (var dependency in applicationDependencyCollection.ApplicationDependencies)
            {
                applicationBuilder.ApplicationDependencies.Add(dependency);
            }

            ApplicationHost applicationHost = applicationBuilder.BuildInternal();
            applicationHost.ConsoleOutput = consoleOutput;

            var scopeFactory = applicationHost.Services.GetRequiredService<IServiceScopeFactory>();
            using var commandScope = scopeFactory.CreateScope();
            InjectServiceProperties(commandInfo, commandScope.ServiceProvider);

            try
            {
                using var hostCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownCts.Token);
                Task commandTask = command.RunInternal(applicationHost, shutdownCts.Token).AsTask();
                Task<int> hostTask = applicationHost.Run(hostCts.Token);

                Task finished = await Task.WhenAny(commandTask, hostTask);

                if (ReferenceEquals(finished, commandTask))
                {
                    // Command finished first: normal return = success, so stop the host.
                    if (commandTask.IsFaulted)
                    {
                        hostCts.Cancel();
                        try { await hostTask; } catch { /* Host outcome is irrelevant: the command failed. */ }
                        await commandTask;
                    }
                    else if (commandTask.IsCanceled)
                    {
                        hostCts.Cancel();
                        try { await hostTask; } catch { /* Host outcome is irrelevant: the command was canceled. */ }
                        await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
                        ThrowIfExternalAbort(shutdownCts, cancellationToken, ctrlCCanceled);
                        await commandTask;
                    }
                    else
                    {
                        await commandTask;
                        hostCts.Cancel();
                        try { await hostTask; } catch (OperationCanceledException) { /* Expected: framework stopped the host after command success. */ }
                    }
                }
                else
                {
                    // Host stopped first (e.g. IHost.StopAsync): drain the command so a
                    // faulted/canceled command is still observed, then honor its outcome.
                    if (hostTask.IsFaulted)
                    {
                        try { await commandTask; } catch { /* Command outcome is irrelevant: the host failed. */ }
                        _ = await hostTask;
                    }
                    else if (hostTask.IsCanceled)
                    {
                        try { await commandTask; } catch { /* Command outcome is irrelevant: the host was canceled. */ }
                        await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
                        _ = await hostTask;
                    }
                    else
                    {
                        int hostExitCode = await hostTask;
                        await commandTask;
                        if (hostExitCode != 0)
                        {
                            throw new CommandException($"Command '{commandInfo.FullCommandName}' exited with code {hostExitCode}", hostExitCode);
                        }
                    }
                }

                await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
            }
            finally
            {
                // Ensure we clean up the lifetime service on both 0 and 130 paths.
                // Null-guarded: service may not exist on early-abort paths.
                await (lifetimeGlobalService?.InvokeApplicationExitedCallbacksAsync() ?? Task.CompletedTask);
            }
        }
        catch (OperationCanceledException ex) when (ex is not ExternalCancellationException && shutdownCts.IsCancellationRequested && (cancellationToken.IsCancellationRequested || ctrlCCanceled))
        {
            // Cancellation was requested via the passed token or Ctrl+C.
            throw new ExternalCancellationException();
        }
        finally
        {
            if (cancelKeyPressSubscribed)
            {
                consoleOutput.CancelKeyPress -= cancelKeyPressHandler;
            }
        }
    }

    /// <summary>
    /// Throws <see cref="ExternalCancellationException"/> when cancellation
    /// was requested via the passed token or Ctrl+C.
    /// </summary>
    private static void ThrowIfExternalAbort(CancellationTokenSource shutdownCts, CancellationToken outerToken, bool ctrlCCanceled)
    {
        if (shutdownCts.IsCancellationRequested && (outerToken.IsCancellationRequested || ctrlCCanceled))
        {
            throw new ExternalCancellationException();
        }
    }

    /// <summary>
    /// Injects per-command services into <see cref="SubCommandInfo.Command"/>
    /// properties marked with the framework service attributes.
    /// CLI-bound properties are never touched: any property carrying both a
    /// CLI marker (<see cref="CommandOptionAttribute"/> /
    /// <see cref="CommandArgumentAttribute"/>) and a service marker is a
    /// configuration error. Values resolve from the per-command scope so
    /// scoped lifetimes stay isolated to one command run.
    /// <para>
    /// Reuse-only seam: no new attribute types. Both markers bind by
    /// attribute simple name so this library gains no new package dependency:
    /// <c>FromServicesAttribute</c> (ASP.NET Core, property-targeted and
    /// directly usable) and <c>FromKeyedServicesAttribute</c> (already
    /// referenced via <c>Microsoft.Extensions.DependencyInjection.Abstractions</c>
    /// for the <c>Key</c> read and the keyed resolution call).
    /// </para>
    /// <para>
    /// Framework limitation: the upstream <c>FromKeyedServicesAttribute</c>
    /// declares <c>AttributeTargets.Parameter</c> only, so the C# compiler
    /// rejects direct property use (CS0592). The keyed path below still
    /// resolves any property attribute named <c>FromKeyedServicesAttribute</c>
    /// that exposes a <c>Key</c> property (same-named shim, emitted metadata,
    /// or a future framework retargeting to properties).
    /// </para>
    /// </summary>
    internal static void InjectServiceProperties(SubCommandInfo commandInfo, IServiceProvider scopedProvider)
    {
        var command = commandInfo.Command!;
        var commandType = command.GetType();
        var cliBoundNames = commandInfo.AllOptions.Select(o => o.Property.Name).Concat(commandInfo.AllArguments.Select(a => a.Property.Name)).ToHashSet(StringComparer.Ordinal);

        foreach (var property in commandType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var attributes = property.GetCustomAttributes(inherit: true);
            bool hasFromServices = attributes.Any(a => a.GetType().Name == "FromServicesAttribute");
            var fromKeyed = attributes.FirstOrDefault(a => a.GetType().Name == "FromKeyedServicesAttribute");
            if (!hasFromServices && fromKeyed is null)
            {
                continue;
            }

            if (cliBoundNames.Contains(property.Name)
                || property.IsDefined(typeof(CommandOptionAttribute), inherit: true)
                || property.IsDefined(typeof(CommandArgumentAttribute), inherit: true))
            {
                throw new InvalidOperationException(
                    $"Property '{commandType.FullName}.{property.Name}' is marked with both a command-line attribute and a service attribute. A property is either CLI-bound or service-injected, never both.");
            }

            if (!property.CanWrite || property.SetMethod is null || property.SetMethod.IsStatic)
            {
                throw new InvalidOperationException(
                    $"Property '{commandType.FullName}.{property.Name}' is marked for service injection but has no writable instance setter.");
            }

            object? value;
            if (fromKeyed is not null)
            {
                object? key = fromKeyed is FromKeyedServicesAttribute typed ? typed.Key : ReadKeyedServiceKey(property, commandType);
                value = scopedProvider.GetRequiredKeyedService(property.PropertyType, key);
            }
            else
            {
                value = scopedProvider.GetRequiredService(property.PropertyType);
            }

            property.SetValue(command, value);
        }
    }

    /// <summary>
    /// Reads the <c>Key</c> of a same-named <c>FromKeyedServicesAttribute</c>
    /// shim from attribute metadata (constructor argument or named argument),
    /// without reflecting over the shim type itself (trim-safe).
    /// </summary>
    private static object? ReadKeyedServiceKey(PropertyInfo property, Type commandType)
    {
        foreach (var data in property.GetCustomAttributesData())
        {
            if (!string.Equals(data.AttributeType.Name, "FromKeyedServicesAttribute", StringComparison.Ordinal))
            {
                continue;
            }

            if (data.ConstructorArguments.Count > 0)
            {
                return data.ConstructorArguments[0].Value;
            }

            foreach (var named in data.NamedArguments)
            {
                if (string.Equals(named.MemberName, "Key", StringComparison.Ordinal))
                {
                    return named.TypedValue.Value;
                }
            }

            throw new InvalidOperationException(
                $"Property '{commandType.FullName}.{property.Name}' is marked with 'FromKeyedServicesAttribute' but carries no key.");
        }

        throw new InvalidOperationException(
            $"Property '{commandType.FullName}.{property.Name}' is marked with 'FromKeyedServicesAttribute' but carries no key.");
    }
}
