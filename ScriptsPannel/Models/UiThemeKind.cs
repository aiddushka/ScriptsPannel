namespace ScriptsPannel.Models;

public enum UiThemeKind
{
    Light,
    Dark,
    /// <summary>Legacy settings only; treated as <see cref="Dark"/> with optional shelf image.</summary>
    CustomBackground
}
