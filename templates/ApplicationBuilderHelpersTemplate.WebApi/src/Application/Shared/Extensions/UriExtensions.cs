namespace Application.Shared.Extensions;

public static class UriExtensions
{
    public static Uri RedactAllQuery(this Uri uri)
    {
        return new Uri(uri.GetLeftPart(UriPartial.Path));
    }

    public static Uri RedactQuery(this Uri uri)
    {
        return new Uri(uri.Query.Length > 0
            ? uri.GetLeftPart(UriPartial.Path) + "?" + uri.Query.RedactQuery()
            : uri.GetLeftPart(UriPartial.Path));
    }

    public static string RedactQuery(this string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        var queryParts = query.Split('&');
        for (int i = 0; i < queryParts.Length; i++)
        {
            var keyValue = queryParts[i].Split('=');
            if (keyValue.Length == 2)
            {
                queryParts[i] = $"{keyValue[0]}=[REDACTED]";
            }
        }
        return string.Join("&", queryParts);
    }
}
