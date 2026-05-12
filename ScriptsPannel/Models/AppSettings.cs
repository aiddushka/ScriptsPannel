namespace ScriptsPannel.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "ru";
    public UiThemeKind Theme { get; set; } = UiThemeKind.Light;
    /// <summary>When true, <see cref="CustomShelfBackgroundPath"/> is drawn behind shelves (light or dark theme).</summary>
    public bool UseShelfBackgroundImage { get; set; }
    /// <summary>Absolute path to background image file.</summary>
    public string? CustomShelfBackgroundPath { get; set; }
    /// <summary>Root folder for shelves/scripts. If empty, defaults next to executable.</summary>
    public string? ScriptsDataRoot { get; set; }
}
