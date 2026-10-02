using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal static class CommandLineTokenizer
{
    internal static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char? quote = null;
        var hasToken = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quote.HasValue)
            {
                if (c == quote.Value)
                    quote = null;
                else
                    current.Append(c);
                hasToken = true;
            }
            else if (c == '"' || c == '\'')
            {
                quote = c;
                hasToken = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (hasToken)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
            }
            else
            {
                current.Append(c);
                hasToken = true;
            }
        }

        if (hasToken)
            tokens.Add(current.ToString());

        return tokens;
    }
}
