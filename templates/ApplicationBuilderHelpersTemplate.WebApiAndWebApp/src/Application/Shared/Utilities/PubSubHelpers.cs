namespace Application.Shared.Utilities;

public static class PubSubHelpers
{
    public static bool IsTopicMatch(string topic, string topicFilter)
    {
        // Handle null or empty inputs
        if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(topicFilter))
            return false;

        // Topics starting with $ are special system topics and don't match # wildcard
        if (topic.StartsWith('$') && topicFilter.StartsWith('#'))
            return false;

        // Split both topic and filter into levels
        var topicLevels = topic.Split('/');
        var filterLevels = topicFilter.Split('/');

        int topicIndex = 0;
        int filterIndex = 0;

        while (filterIndex < filterLevels.Length && topicIndex < topicLevels.Length)
        {
            string filterLevel = filterLevels[filterIndex];

            // Multi-level wildcard (#) - must be the last level in filter
            if (filterLevel == "#")
            {
                // # must be the last character in the filter
                return filterIndex == filterLevels.Length - 1;
            }

            // Single-level wildcard (+) - matches exactly one level
            if (filterLevel == "+")
            {
                // + matches any single level except empty levels
                if (string.IsNullOrEmpty(topicLevels[topicIndex]))
                    return false;
            }
            else
            {
                // Exact match required
                if (filterLevel != topicLevels[topicIndex])
                    return false;
            }

            topicIndex++;
            filterIndex++;
        }

        // Check if we've consumed all levels in both topic and filter
        return topicIndex == topicLevels.Length && filterIndex == filterLevels.Length;
    }
}
