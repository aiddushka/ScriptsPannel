namespace ScriptsPannel.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "ru";
    public UiThemeKind Theme { get; set; } = UiThemeKind.Light;
    /// <summary>Absolute path to image for CustomBackground theme.</summary>
    public string? CustomShelfBackgroundPath { get; set; }
    /// <summary>Root folder for shelves/scripts. If empty, defaults next to executable.</summary>
    public string? ScriptsDataRoot { get; set; }
}
