namespace ApplicationBuilderHelpers.Common.Internals;

/// <summary>Shared trim-message constants.</summary>
internal class Message
{
    /// <summary>Trim-preservation message for serializable objects.</summary>
    internal const string RequiresUnreferencedCodeMessage = $"Serializable objects must preserve all its required types when trimming is enabled";
}
