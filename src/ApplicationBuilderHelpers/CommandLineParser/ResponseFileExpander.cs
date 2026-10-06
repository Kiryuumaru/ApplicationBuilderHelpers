using ApplicationBuilderHelpers.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal static class ResponseFileExpander
{
    internal const int MaxDepth = 8;
    internal const long MaxSingleFileBytes = 1024 * 1024;
    internal const long MaxTotalBytes = 4 * 1024 * 1024;
    internal const int MaxExpandedArgs = 10000;

    internal static string[] Expand(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args, nameof(args));
        var output = new List<string>(args.Length);
        var totalBytes = 0L;
        var separatorSeen = false;
        foreach (var token in args)
        {
            if (!separatorSeen && string.Equals(token, "--", StringComparison.Ordinal))
            {
                separatorSeen = true;
                output.Add(token);
                continue;
            }

            if (separatorSeen)
            {
                output.Add(token);
                continue;
            }

            if (token.StartsWith("@@", StringComparison.Ordinal))
            {
                output.Add(token[1..]);
                continue;
            }

            if (token.StartsWith('@'))
            {
                var chain = NewChain();
                var expanded = ExpandToken(token, depth: 0, chain, ref totalBytes);
                output.AddRange(expanded);
                continue;
            }

            output.Add(token);
        }

        if (output.Count > MaxExpandedArgs)
            throw ExpansionFault($"Response file expansion exceeds {MaxExpandedArgs} arguments.");

        return [.. output];
    }

    private static HashSet<string> NewChain() =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.Ordinal);

    private static List<string> ExpandToken(string token, int depth, HashSet<string> chain, ref long totalBytes)
    {
        var path = token[1..];
        if (path.Length == 0)
            throw ExpansionFault("Response file '@' names no file.");
        return ExpandFile(path, depth, chain, ref totalBytes);
    }

    private static List<string> ExpandFile(string path, int depth, HashSet<string> chain, ref long totalBytes)
    {
        if (depth >= MaxDepth)
            throw ExpansionFault($"Response file '{path}' exceeds nesting depth {MaxDepth}.");
        var fullPath = ResolveFullPath(path);
        if (!chain.Add(fullPath))
            throw ExpansionFault($"Response file '{path}' forms a cycle.");
        try
        {
            long length;
            bool exists;
            try
            {
                var info = new FileInfo(fullPath);
                exists = info.Exists;
                length = exists ? info.Length : 0;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                throw ExpansionFault($"Response file '{path}' is unreadable: {ex.Message}");
            }

            if (!exists)
                throw ExpansionFault($"Response file '{path}' not found.");
            if (length > MaxSingleFileBytes)
                throw ExpansionFault($"Response file '{path}' exceeds size limit.");
            totalBytes += length;
            if (totalBytes > MaxTotalBytes)
                throw ExpansionFault("Response file expansion exceeds total size limit.");
            string content;
            try
            {
                content = File.ReadAllText(fullPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                throw ExpansionFault($"Response file '{path}' is unreadable: {ex.Message}");
            }

            var tokens = CommandLineTokenizer.Tokenize(content);
            var output = new List<string>(tokens.Count);
            foreach (var child in tokens)
            {
                if (child.StartsWith("@@", StringComparison.Ordinal))
                {
                    output.Add(child[1..]);
                    continue;
                }

                if (child.StartsWith('@'))
                {
                    output.AddRange(ExpandToken(child, depth + 1, chain, ref totalBytes));
                    continue;
                }

                output.Add(child);
            }

            return output;
        }
        finally
        {
            chain.Remove(fullPath);
        }
    }

    private static string ResolveFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw ExpansionFault($"Response file '{path}' is unreadable: {ex.Message}");
        }
    }

    private static CommandException ExpansionFault(string message) =>
        new(message, 1, CommandErrorKind.Fault);
}
