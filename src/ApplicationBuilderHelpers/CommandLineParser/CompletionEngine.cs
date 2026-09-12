using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Tolerant shell-completion over the <see cref="SubCommandInfo"/> hierarchy.
/// Never throws: null inputs and malformed tokens yield an empty list.
/// Secret values are never suggested.
/// </summary>
internal static class CompletionEngine
{
    internal static IReadOnlyList<string> Complete(SubCommandInfo? root, string[]? args, string? partial = null)
    {
        try
        {
            if (root == null)
                return [];

            args ??= [];
            partial ??= string.Empty;

            var target = root;
            var consumed = 0;
            while (consumed < args.Length)
            {
                var token = args[consumed];
                if (string.IsNullOrEmpty(token) || token.StartsWith('-'))
                    break;

                var child = target.FindChild(token);
                if (child == null)
                    break;

                target = child;
                consumed++;
            }

            var remaining = args.Skip(consumed).ToArray();

            // 1. "--opt=val" value completion.
            var equals = partial.IndexOf('=');
            if (equals >= 0 && partial.StartsWith('-'))
            {
                var namePart = partial[..equals];
                var valuePrefix = partial[(equals + 1)..];
                var option = FindOption(target.AllOptions, namePart);
                return CompleteValidValues(option?.ValidValues, option?.IsSecret == true, valuePrefix, partial[..(equals + 1)]);
            }

            // 2. Previous token is a valued option awaiting its value ("--mode <TAB>").
            if (remaining.Length > 0 && string.IsNullOrEmpty(partial))
            {
                var previous = remaining[^1];
                var option = FindOption(target.AllOptions, previous);
                if (option != null && !option.IsFlag)
                    return CompleteValidValues(option.ValidValues, option.IsSecret, string.Empty, string.Empty);
            }

            if (!string.IsNullOrEmpty(partial) && remaining.Length > 0)
            {
                var previous = remaining[^1];
                var option = FindOption(target.AllOptions, previous);
                if (option != null && !option.IsFlag && !previous.Contains('='))
                    return CompleteValidValues(option.ValidValues, option.IsSecret, partial, string.Empty);
            }

            // 3. Option-name completion.
            if (partial.StartsWith('-'))
            {
                var prefix = partial;
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var option in target.AllOptions)
                {
                    if (option.LongName != null)
                    {
                        var longForm = $"--{option.LongName}";
                        if (longForm.StartsWith(prefix, StringComparison.Ordinal))
                            names.Add(longForm);
                    }

                    if (option.ShortName.HasValue)
                    {
                        var shortForm = $"-{option.ShortName.Value}";
                        if (shortForm.StartsWith(prefix, StringComparison.Ordinal))
                            names.Add(shortForm);
                    }
                }

                return [.. names.OrderBy(n => n, StringComparer.Ordinal)];
            }

            // 4. Subcommand-name completion.
            var matches = target.Children.Keys
                .Where(name => name.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (matches.Count > 0)
                return matches;

            // 5. Positional-argument ValidValues fallback.
            var position = remaining.Count(t => !t.StartsWith('-'));
            if (!string.IsNullOrEmpty(partial) && remaining.Length > 0 && !remaining[^1].StartsWith('-'))
                position = Math.Max(0, position - 1);

            foreach (var argument in target.AllArguments)
            {
                if (!argument.CanAcceptValueAtPosition(position))
                    continue;

                var values = CompleteValidValues(argument.ValidValues, argument.IsSecret, partial, string.Empty);
                if (values.Count > 0)
                    return values;
            }

            return [];
        }
        catch
        {
            return [];
        }
    }

    private static SubCommandOptionInfo? FindOption(List<SubCommandOptionInfo> options, string token)
    {
        try
        {
            var namePart = token.Contains('=') ? token[..token.IndexOf('=')] : token;
            foreach (var option in options)
            {
                try
                {
                    if (option.MatchesArgument(token) || option.MatchesArgument(namePart))
                        return option;
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> CompleteValidValues(object[]? validValues, bool isSecret, string prefix, string valuePrefix)
    {
        try
        {
            if (isSecret || validValues == null || validValues.Length == 0)
                return [];

            var results = new List<string>();
            foreach (var value in validValues)
            {
                string? text;
                try
                {
                    text = value?.ToString();
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrEmpty(text))
                    continue;

                if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    results.Add($"{valuePrefix}{text}");
            }

            return results;
        }
        catch
        {
            return [];
        }
    }
}
