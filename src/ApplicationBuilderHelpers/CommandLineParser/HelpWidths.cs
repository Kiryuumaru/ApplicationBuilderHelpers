namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Single home for help width bounds: default 120, floor 60, ceiling 1024.
/// </summary>
internal static class HelpWidths
{
    internal const int Default = 120;
    internal const int Minimum = 60;
    internal const int Maximum = 1024;

    internal static int Clamp(int helpWidth) =>
        System.Math.Min(System.Math.Max(helpWidth, Minimum), Maximum);

    internal static void ThrowIfOutOfRange(int helpWidth)
    {
        if (helpWidth < 1 || helpWidth > Maximum)
        {
            throw new System.ArgumentOutOfRangeException(nameof(helpWidth), $"Help width must be between 1 and {Maximum}.");
        }
    }
}
