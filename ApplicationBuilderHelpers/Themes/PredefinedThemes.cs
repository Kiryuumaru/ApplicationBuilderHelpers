using ApplicationBuilderHelpers.Interfaces;

namespace ApplicationBuilderHelpers.Themes;

/// <summary>
/// Monokai Dimmed color theme - stylish, expressive, and great contrast.
/// </summary>
public class MonokaiDimmedTheme : IAnsiTheme
{
    public static MonokaiDimmedTheme Instance { get; } = new MonokaiDimmedTheme();

    public string HeaderColor => "\u001b[38;2;249;38;114m";     // Magenta #F92672
    public string FlagColor => "\u001b[38;2;230;219;116m";      // Yellow #E6DB74  
    public string ParameterColor => "\u001b[38;2;102;217;239m"; // Aqua #66D9EF
    public string DescriptionColor => "\u001b[38;2;248;248;242m"; // White #F8F8F2
    public string SecondaryColor => "\u001b[38;2;117;113;94m";  // Gray #75715E
    public string RequiredColor => "\u001b[38;2;253;151;31m";   // Orange-Red #FD971F
    public string Reset => "\u001b[0m";
}

/// <summary>
/// Dracula color theme - dark and mysterious.
/// </summary>
public class DraculaTheme : IAnsiTheme
{
    public static DraculaTheme Instance { get; } = new DraculaTheme();

    public string HeaderColor => "\u001b[38;2;255;121;198m";    // Pink #FF79C6
    public string FlagColor => "\u001b[38;2;241;250;140m";      // Yellow #F1FA8C
    public string ParameterColor => "\u001b[38;2;139;233;253m"; // Cyan #8BE9FD
    public string DescriptionColor => "\u001b[38;2;248;248;242m"; // Foreground #F8F8F2
    public string SecondaryColor => "\u001b[38;2;98;114;164m";  // Comment #6272A4
    public string RequiredColor => "\u001b[38;2;255;85;85m";    // Red #FF5555
    public string Reset => "\u001b[0m";
}

/// <summary>
/// Solarized Dark color theme - elegant and readable.
/// </summary>
public class SolarizedDarkTheme : IAnsiTheme
{
    public static SolarizedDarkTheme Instance { get; } = new SolarizedDarkTheme();

    public string HeaderColor => "\u001b[38;2;203;75;22m";      // Orange #CB4B16
    public string FlagColor => "\u001b[38;2;181;137;0m";        // Yellow #B58900
    public string ParameterColor => "\u001b[38;2;42;161;152m";  // Cyan #2AA198
    public string DescriptionColor => "\u001b[38;2;131;148;150m"; // Base0 #839496
    public string SecondaryColor => "\u001b[38;2;88;110;117m";  // Base01 #586E75
    public string RequiredColor => "\u001b[38;2;220;50;47m";    // Red #DC322F
    public string Reset => "\u001b[0m";
}

/// <summary>
/// Nord color theme - minimal and frost-inspired.
/// </summary>
public class NordTheme : IAnsiTheme
{
    public static NordTheme Instance { get; } = new NordTheme();

    public string HeaderColor => "\u001b[38;2;136;192;208m";    // Frost #88C0D0
    public string FlagColor => "\u001b[38;2;235;203;139m";      // Aurora Yellow #EBCB8B
    public string ParameterColor => "\u001b[38;2;129;161;193m"; // Frost #81A1C1
    public string DescriptionColor => "\u001b[38;2;236;239;244m"; // Snow Storm #ECEFF4
    public string SecondaryColor => "\u001b[38;2;143;188;187m"; // Frost #8FBCBB
    public string RequiredColor => "\u001b[38;2;191;97;106m";   // Aurora Red #BF616A
    public string Reset => "\u001b[0m";
}

/// <summary>
/// Gruvbox Dark color theme - warm and retro.
/// </summary>
public class GruvboxDarkTheme : IAnsiTheme
{
    public static GruvboxDarkTheme Instance { get; } = new GruvboxDarkTheme();

    public string HeaderColor => "\u001b[38;2;214;93;14m";      // Orange #D65D0E
    public string FlagColor => "\u001b[38;2;250;189;47m";       // Yellow #FABD2F
    public string ParameterColor => "\u001b[38;2;131;165;152m"; // Aqua #83A598
    public string DescriptionColor => "\u001b[38;2;235;219;178m"; // Light #EBDBB2
    public string SecondaryColor => "\u001b[38;2;146;131;116m"; // Gray #928374
    public string RequiredColor => "\u001b[38;2;251;73;52m";    // Red #FB4934
    public string Reset => "\u001b[0m";
}

/// <summary>
/// Visual Studio Code Dark+ theme - familiar and professional colors inspired by the popular editor.
/// </summary>
public class VSCodeDarkTheme : IAnsiTheme
{
    public static VSCodeDarkTheme Instance { get; } = new VSCodeDarkTheme();

    public string HeaderColor => "\u001b[38;2;78;201;176m";     // Teal #4EC9B0 (interface/type color)
    public string FlagColor => "\u001b[38;2;86;156;214m";       // Blue #569CD6 (keyword color)
    public string ParameterColor => "\u001b[38;2;220;220;170m"; // Light Yellow #DCDCAA (parameter color)
    public string DescriptionColor => "\u001b[38;2;212;212;212m"; // Light Gray #D4D4D4 (foreground)
    public string SecondaryColor => "\u001b[38;2;106;153;85m";  // Green #6A9955 (comment color)
    public string RequiredColor => "\u001b[38;2;244;71;71m";    // Red #F44747 (error color)
    public string Reset => "\u001b[0m";
}

/// <summary>
/// Visual Studio Code Light+ theme - clean and bright colors for light terminal backgrounds.
/// </summary>
public class VSCodeLightTheme : IAnsiTheme
{
    public static VSCodeLightTheme Instance { get; } = new VSCodeLightTheme();

    public string HeaderColor => "\u001b[38;2;43;145;175m";     // Dark Teal #2B91AF (interface/type color)
    public string FlagColor => "\u001b[38;2;0;0;255m";          // Blue #0000FF (keyword color)
    public string ParameterColor => "\u001b[38;2;121;94;38m";   // Brown #795E26 (parameter color)
    public string DescriptionColor => "\u001b[38;2;0;0;0m";     // Black #000000 (foreground)
    public string SecondaryColor => "\u001b[38;2;0;128;0m";     // Green #008000 (comment color)
    public string RequiredColor => "\u001b[38;2;205;49;49m";    // Red #CD3131 (error color)
    public string Reset => "\u001b[0m";
}

/// <summary>
/// Classic theme - simple and traditional colors.
/// </summary>
public class ClassicTheme : IAnsiTheme
{
    public static ClassicTheme Instance { get; } = new ClassicTheme();

    public string HeaderColor => "\u001b[1;33m";    // Bold Yellow
    public string FlagColor => "\u001b[1;32m";      // Bold Green
    public string ParameterColor => "\u001b[1;36m"; // Bold Cyan
    public string DescriptionColor => "\u001b[0m";  // Default
    public string SecondaryColor => "\u001b[90m";   // Bright Black (Gray)
    public string RequiredColor => "\u001b[1;31m";  // Bold Red
    public string Reset => "\u001b[0m";
}

/// <summary>
/// No Color theme - for terminals that don't support colors.
/// </summary>
public class NoColorTheme : IAnsiTheme
{
    public static NoColorTheme Instance { get; } = new NoColorTheme();

    public string HeaderColor => "";
    public string FlagColor => "";
    public string ParameterColor => "";
    public string DescriptionColor => "";
    public string SecondaryColor => "";
    public string RequiredColor => "";
    public string Reset => "";
}