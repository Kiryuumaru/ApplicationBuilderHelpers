using System.Text;

namespace Application.Shared.Extensions;

public static class StringCaseExtensions
{
    /// <summary>
    /// Converts a string from any case to snake_case.
    /// </summary>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = SplitWords(input);
        return string.Join("_", words.Select(w => w.ToLowerInvariant()));
    }

    /// <summary>
    /// Converts a string from any case to PascalCase.
    /// </summary>
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = SplitWords(input);
        var sb = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                    sb.Append(word[1..].ToLowerInvariant());
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts a string from any case to camelCase.
    /// </summary>
    public static string ToCamelCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var pascalCase = ToPascalCase(input);

        if (pascalCase.Length == 0)
            return pascalCase;

        return char.ToLowerInvariant(pascalCase[0]) + pascalCase[1..];
    }

    /// <summary>
    /// Converts a string from any case to kebab-case.
    /// </summary>
    public static string ToKebabCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = SplitWords(input);
        return string.Join("-", words.Select(w => w.ToLowerInvariant()));
    }

    /// <summary>
    /// Splits a string into words, handling PascalCase, camelCase, snake_case, kebab-case, and space-separated input.
    /// </summary>
    private static List<string> SplitWords(string input)
    {
        var words = new List<string>();
        var currentWord = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            // Handle delimiters (space, underscore, hyphen)
            if (c == ' ' || c == '_' || c == '-')
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                continue;
            }

            // Handle uppercase letters (PascalCase/camelCase detection)
            if (char.IsUpper(c))
            {
                // Start a new word if:
                // 1. Current word is not empty
                // 2. Next char is lowercase (e.g., "XMLParser" -> "XML" "Parser")
                // 3. Or this is just a normal case boundary
                if (currentWord.Length > 0)
                {
                    // Check if this is an acronym followed by a word (e.g., "XMLParser")
                    if (i + 1 < input.Length && char.IsLower(input[i + 1]) && currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    // Check if previous char was lowercase (normal case boundary)
                    else if (char.IsLower(input[i - 1]))
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                }

                currentWord.Append(c);
            }
            else
            {
                currentWord.Append(c);
            }
        }

        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return [.. words.Where(w => !string.IsNullOrWhiteSpace(w))];
    }
}