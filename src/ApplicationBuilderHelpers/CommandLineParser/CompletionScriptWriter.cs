using System;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Writes dotnet-style shell completion shims that re-invoke
/// <c>myapp complete --position N "&lt;commandline&gt;"</c> on each TAB.
/// N is always a 0-based character offset into the full command-line string
/// (same unit the gateway slices with: <c>commandline[..position]</c>).
/// Stdout only (<see cref="ConsoleOutput"/> Write/WriteLine methods);
/// descriptions are supported by zsh/pwsh shims only, omitted for bash/fish.
/// </summary>
internal static class CompletionScriptWriter
{
    internal static void WriteBash(ConsoleOutput output, string? executableName)
    {
        if (output == null)
            return;

        var exe = NormalizeExeName(executableName);
        var func = ToFuncName(exe);

        output.WriteLine($"# {exe} bash completion (dotnet-style: re-invokes `{exe} complete --position N \"<commandline>\"` per TAB)");
        output.WriteLine($"# N is a 0-based character offset into the full line (--position takes chars, not words).");
        output.WriteLine($"{func}_complete() {{");
        output.WriteLine("  local cur=");
        output.WriteLine("  cur=\"${COMP_WORDS[COMP_CWORD]}\"");
        output.WriteLine("  local commandline=\"$COMP_LINE\"");
        output.WriteLine("  local position=${#commandline}");
        output.WriteLine("  if [ \"$COMP_POINT\" -ge 0 ] 2>/dev/null; then");
        output.WriteLine("    position=$COMP_POINT");
        output.WriteLine("  fi");
        output.WriteLine("  local completions");
        output.WriteLine($"  completions=\"$({exe} complete --position \"$position\" \"$commandline\")\"");
        output.WriteLine("  COMPREPLY=( $(compgen -W \"$completions\" -- \"$cur\") )");
        output.WriteLine("  return 0");
        output.WriteLine("}");
        output.WriteLine($"complete -F {func}_complete {exe}");
    }

    internal static void WriteZsh(ConsoleOutput output, string? executableName)
    {
        if (output == null)
            return;

        var exe = NormalizeExeName(executableName);
        var func = ToFuncName(exe);

        output.WriteLine($"#compdef {exe}");
        output.WriteLine($"# {exe} zsh completion (dotnet-style: re-invokes `{exe} complete --position N \"<commandline>\"` per TAB)");
        output.WriteLine("# N is a 0-based character offset into the full buffer (--position takes chars, not words).");
        output.WriteLine("# Descriptions shown via _describe with name:description entries.");
        output.WriteLine($"_{func}() {{");
        output.WriteLine("  local -a completions");
        output.WriteLine("  local commandline=\"$BUFFER\"");
        output.WriteLine("  local position=$CURSOR");
        output.WriteLine("  if [ -z \"$position\" ]; then");
        output.WriteLine("    position=${#commandline}");
        output.WriteLine("  fi");
        output.WriteLine("  local raw");
        output.WriteLine($"  raw=\"$({exe} complete --position \"$position\" \"$commandline\")\"");
        output.WriteLine("  completions=(${(@f)raw})");
        output.WriteLine("  _describe -t commands \"" + exe + " completions\" completions");
        output.WriteLine("}");
        output.WriteLine($"compdef _{func} {exe}");
    }

    internal static void WritePwsh(ConsoleOutput output, string? executableName)
    {
        if (output == null)
            return;

        var exe = NormalizeExeName(executableName);

        output.WriteLine($"# {exe} PowerShell completion (dotnet-style: re-invokes `{exe} complete --position N \"<commandline>\"` per TAB)");
        output.WriteLine("# Descriptions shown via CompletionResult tooltips (name`tdescription lines).");
        output.WriteLine($"Register-ArgumentCompleter -Native -CommandName {exe} -ScriptBlock {{");
        output.WriteLine("  param($commandName, $wordToComplete, $cursorPosition)");
        output.WriteLine("  $commandline = $commandAst.ToString()");
        output.WriteLine($"  $raw = & {exe} complete --position $cursorPosition \"$commandline\"");
        output.WriteLine("  foreach ($line in $raw) {");
        output.WriteLine("    $name, $description = $line -split \"`t\", 2");
        output.WriteLine("    if ([string]::IsNullOrEmpty($description)) { $description = $name }");
        output.WriteLine("    [System.Management.Automation.CompletionResult]::new($name, $name, 'ParameterValue', $description)");
        output.WriteLine("  }");
        output.WriteLine("}");
    }

    internal static void WriteFish(ConsoleOutput output, string? executableName)
    {
        if (output == null)
            return;

        var exe = NormalizeExeName(executableName);
        var func = ToFuncName(exe);

        output.WriteLine($"# {exe} fish completion (dotnet-style: re-invokes `{exe} complete --position N \"<commandline>\"` per TAB)");
        output.WriteLine($"# N is a 0-based character offset into the full line (--position takes chars, not token counts).");
        output.WriteLine($"function __{func}_complete");
        output.WriteLine("  set -l commandline (commandline)");
        output.WriteLine("  set -l position (commandline -C)");
        output.WriteLine("  if test -z \"$position\"");
        output.WriteLine("    set position (string length -- \"$commandline\")");
        output.WriteLine("  end");
        output.WriteLine($"  {exe} complete --position $position \"$commandline\"");
        output.WriteLine("end");
        output.WriteLine($"complete -f -c {exe} -a \"(__{func}_complete)\"");
    }

    private static string NormalizeExeName(string? executableName) =>
        string.IsNullOrWhiteSpace(executableName) ? "myapp" : executableName.Trim();

    private static string ToFuncName(string exe)
    {
        var chars = exe.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var name = new string(chars).Trim('_');
        return string.IsNullOrEmpty(name) ? "myapp" : name;
    }
}
